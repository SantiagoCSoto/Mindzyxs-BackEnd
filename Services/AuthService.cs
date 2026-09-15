using Microsoft.Extensions.Options;
using Mindzyxs_API.Dtos;
using Mindzyxs_API.Repositories;
using Mindzyxs_API.Utilities;

namespace Mindzyxs_API.Services;

public class RegistroOptions
{
    public int CodigoExpiraMinutos { get; set; } = 15;
}

public class AuthService(
    IUserRepository users,
    ICatalogRepository catalogs,
    ICodigoRepository codes,
    ITarjetaStorage tarjetas,
    IEmailSender email,
    IOptions<RegistroOptions> registroOptions,
    ILogger<AuthService> logger)
{
    // PB-01 HU1+ HU2 + HU3: valida, crea en PendienteVerificacion y genera código HU4.
    public async Task<OperationResult<int>> RegisterAsync(RegisterVm vm, CancellationToken ct = default)
    {
        if (vm is null) return OperationResult<int>.Fail("Solicitud inválida.");

        // HU1-PB1: tipo obligatorio, solo Paciente/Psicologo.
        if (!ContactHelper.IsValidTipoUsuario(vm.TipoUsuario))
            return OperationResult<int>.Fail("Debes seleccionar un tipo de cuenta: Paciente o Psicólogo");
        var tipo = ContactHelper.CanonicalTipoUsuario(vm.TipoUsuario);
        var esPsicologo = tipo == AppRoles.Psicologo;

        // HU2-PB1: obligatorios y formatos.
        if (string.IsNullOrWhiteSpace(vm.Nombre) || string.IsNullOrWhiteSpace(vm.Apellido))
            return OperationResult<int>.Fail("Este campo es obligatorio");
        if (vm.Edad is null or < 1 or > 120)
            return OperationResult<int>.Fail("Ingresa una edad válida");
        if (vm.IdGenero is null || !await catalogs.ExisteGeneroAsync(vm.IdGenero.Value, ct))
            return OperationResult<int>.Fail("Este campo es obligatorio");

        var correo = ContactHelper.Normalize(vm.Correo);
        if (string.IsNullOrWhiteSpace(correo))
            return OperationResult<int>.Fail("Este campo es obligatorio");
        if (!ContactHelper.IsEmail(correo))
            return OperationResult<int>.Fail("Ingresa un correo válido");
        if (await users.ExistsByCorreoAsync(correo, ct))
            return OperationResult<int>.Fail("El correo ya se encuentra registrado");

        if (string.IsNullOrWhiteSpace(vm.Password) || !PasswordHelper.IsStrongEnough(vm.Password))
            return OperationResult<int>.Fail("La contraseña debe tener mínimo 6 caracteres e incluir al menos una letra y un número");

        // HU3-PB1: tarjeta obligatoria/PDF/5MB solo psicólogo.
        if (tarjetas.Validate(vm.TarjetaProfesional, esPsicologo, out var fileError) != null)
            return OperationResult<int>.Fail(fileError!);

        var idRol = await catalogs.GetRolIdAsync(tipo, ct);
        var idPendVerif = await catalogs.GetEstadoIdAsync(AccountStates.PendienteVerificacion, ct);
        int? idAprob = esPsicologo
            ? await catalogs.GetEstadoIdAsync(AccountStates.PendienteAprobacion, ct)
            : null;

        var id = await users.AddAsync(new UsuarioDto
        {
            Nombre = vm.Nombre.Trim(),
            Apellido = vm.Apellido.Trim(),
            Correo = correo,
            PasswordHash = PasswordHelper.Hash(vm.Password),
            FechaNacimiento = AgeHelper.ToBirthDate(vm.Edad.Value),
            IdRol = idRol,
            IdEstadoCuenta = idPendVerif,
            IdEstadoAprobacion = idAprob,
            IdGenero = vm.IdGenero
        }, ct);

        if (esPsicologo && vm.TarjetaProfesional is not null)
        {
            try
            {
                var path = await tarjetas.SaveAsync(vm.TarjetaProfesional, id, ct);
                await users.SetFilePathAsync(id, path, ct);
            }
            catch (InvalidOperationException ex)
            {
                return OperationResult<int>.Fail(ex.Message);
            }
        }

        var codigo = CodeHelper.NewCode6();
        var expira = DateTimeProvider.UtcNow().AddMinutes(registroOptions.Value.CodigoExpiraMinutos);
        await codes.CrearAsync(id, codigo, VerificationTypes.Registro, expira, ct);

        try
        {
            await email.SendAsync(correo, "Mindzyxs - Código de verificación",
                EmailTemplates.VerificationCode(vm.Nombre.Trim(), codigo), ct);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "No se pudo enviar el código a {Correo}", correo);
        }

        return OperationResult<int>.Ok(id, "Registro exitoso. Verifica tu cuenta.");
    }
}
