using Microsoft.Extensions.Options;
using Mindzyxs_API.Dtos;
using Mindzyxs_API.Repositories;
using Mindzyxs_API.Services;
using Mindzyxs_API.Utilities;

namespace Mindzyxs_API.Services;

public class PerfilService(
    IUserRepository users,
    ICatalogRepository catalogs,
    ICodigoRepository codes,
    ISesionRepository sesiones,
    IAuthLogRepository authLogs,
    IEmailSender email,
    IOptions<RegistroOptions> registroOptions)
{
    // PB-10 HU1-PB10: actualiza nombres, apellidos, fecha de nacimiento y género.
    // Valida obligatorios, fecha válida (no futura, mayor de 14) y género existente.
    // Lanza InvalidOperationException con el mensaje exacto para el 400.
    public async Task<string> ActualizarAsync(string? tokenRaw, ActualizarPerfilVm vm, string? ip, CancellationToken ct = default)
    {
        var yo = await RequireSesionAsync(tokenRaw, ct);
        var nombres = (vm.Nombres ?? "").Trim();
        var apellidos = (vm.Apellidos ?? "").Trim();
        if (string.IsNullOrWhiteSpace(nombres) || string.IsNullOrWhiteSpace(apellidos))
            throw new InvalidOperationException("Este campo es obligatorio");
        if (vm.FechaNacimiento.HasValue)
        {
            var fn = vm.FechaNacimiento.Value.Date;
            var hoy = DateTime.UtcNow.Date;
            if (fn > hoy) throw new InvalidOperationException("Ingresa una fecha válida");
            var edad = hoy.Year - fn.Year - (hoy < fn.AddYears(hoy.Year - fn.Year) ? 1 : 0);
            if (edad < 14) throw new InvalidOperationException("Ingresa una edad válida");
        }
        if (vm.IdGenero.HasValue && !await catalogs.ExisteGeneroAsync(vm.IdGenero.Value, ct))
            throw new InvalidOperationException("Selecciona un género válido");

        await users.ActualizarPerfilAsync(yo.IdUsuario, nombres, apellidos, vm.FechaNacimiento, vm.IdGenero, ct);
        await authLogs.LogAsync(yo.IdUsuario, yo.IdUsuario, LogAcciones.PerfilActualizado,
            $"Paciente:{yo.IdUsuario} actualizó su perfil", ip, ct);
        return "Perfil actualizado correctamente";
    }

    // PB-10 HU1-PB10: envía código de verificación al nuevo correo.
    // Si ya está registrado -> 409 "El correo ya se encuentra registrado".
    public async Task<string> SolicitarCambioAsync(string? tokenRaw, string nuevoCorreoRaw, string? ip, CancellationToken ct = default)
    {
        var yo = await RequireSesionAsync(tokenRaw, ct);
        var nuevo = ContactHelper.Normalize(nuevoCorreoRaw);
        if (!ContactHelper.IsEmail(nuevo))
            throw new InvalidOperationException("Ingresa un correo válido");
        if (await users.ExistsByCorreoAsync(nuevo, ct))
            throw new InvalidOperationException("El correo ya se encuentra registrado");

        var codigo = CodeHelper.NewCode6();
        var expira = DateTimeProvider.UtcNow().AddMinutes(registroOptions.Value.CodigoExpiraMinutos);
        await codes.CrearAsync(yo.IdUsuario, codigo, VerificationTypes.CambioCorreo, expira, ct);
        await email.SendAsync(nuevo, "Mindzyxs - Código para cambio de correo",
            EmailTemplates.VerificationCode(yo.Nombre, codigo), ct);
        await authLogs.LogAsync(yo.IdUsuario, yo.IdUsuario, LogAcciones.CorreoSolicitado,
            $"Paciente:{yo.IdUsuario} solicitó cambio de correo a {nuevo}", ip, ct);
        return "Código enviado";
    }

    // PB-10 HU1-PB10: valida el código y aplica el cambio de correo.
    public async Task<string> ConfirmarCambioAsync(string? tokenRaw, string nuevoCorreoRaw, string codigoRaw, string? ip, CancellationToken ct = default)
    {
        var yo = await RequireSesionAsync(tokenRaw, ct);
        var nuevo = ContactHelper.Normalize(nuevoCorreoRaw);
        if (!ContactHelper.IsEmail(nuevo))
            throw new InvalidOperationException("Ingresa un correo válido");
        if (await users.ExistsByCorreoAsync(nuevo, ct))
            throw new InvalidOperationException("El correo ya se encuentra registrado");

        var ultimo = await codes.UltimoAsync(yo.IdUsuario, VerificationTypes.CambioCorreo, ct);
        if (ultimo is null)
            throw new InvalidOperationException("Código incorrecto. Verifica e intenta nuevamente");
        if (DateTimeProvider.UtcNow() > ultimo.ExpiraEn)
            throw new InvalidOperationException("El código expiró. Reenvía el código");
        if (!string.Equals(ultimo.Codigo, (codigoRaw ?? "").Trim(), StringComparison.Ordinal))
            throw new InvalidOperationException("Código incorrecto. Verifica e intenta nuevamente");

        await codes.ConsumirAsync(ultimo.IdCodigo, ct);
        await users.ActualizarCorreoAsync(yo.IdUsuario, nuevo, ct);
        await authLogs.LogAsync(yo.IdUsuario, yo.IdUsuario, LogAcciones.CorreoActualizado,
            $"Paciente:{yo.IdUsuario} cambió su correo a {nuevo}", ip, ct);
        return "Correo actualizado correctamente";
    }

    // Registro incompleto (cerró/recargó sin verificar): si sigue pendiente,
    // reenvía código y avisa para continuar; si no, mensaje genérico (sin filtrar).
    public async Task<(bool Requiere, string Mensaje)> ReanudarAsync(string? correoRaw, CancellationToken ct = default)
    {
        const string generico = "Si el correo está registrado y pendiente, recibirás un código.";
        var c = ContactHelper.Normalize(correoRaw);
        if (!ContactHelper.IsEmail(c)) return (false, generico);
        var user = await users.FindByCorreoAsync(c, ct);
        if (user is null) return (false, generico);
        var estado = await users.GetEstadoCuentaNombreAsync(user.IdEstadoCuenta, ct);
        if (!string.Equals(estado, AccountStates.PendienteVerificacion, StringComparison.OrdinalIgnoreCase))
            return (false, generico);

        var codigo = CodeHelper.NewCode6();
        var expira = DateTimeProvider.UtcNow().AddMinutes(registroOptions.Value.CodigoExpiraMinutos);
        await codes.CrearAsync(user.IdUsuario, codigo, VerificationTypes.Registro, expira, ct);
        await email.SendAsync(user.Correo, "Mindzyxs - Nuevo código de verificación",
            EmailTemplates.VerificationCode(user.Nombre, codigo), ct);
        return (true, "Te enviamos un nuevo código. Continúa con tu verificación.");
    }

    private async Task<UsuarioDto> RequireSesionAsync(string? tokenRaw, CancellationToken ct)
    {
        if (string.IsNullOrWhiteSpace(tokenRaw))
            throw new UnauthorizedAccessException("Sesión requerida");
        var sesion = await sesiones.FindByTokenHashAsync(TokenHelper.Hash(tokenRaw.Trim()), ct);
        if (sesion is null || sesion.ExpiraEn < DateTime.UtcNow)
            throw new UnauthorizedAccessException("Sesión requerida");
        return await users.FindByIdAsync(sesion.IdUsuario, ct)
            ?? throw new UnauthorizedAccessException("Sesión requerida");
    }
}
