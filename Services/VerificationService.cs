using Microsoft.Extensions.Options;
using Mindzyxs_API.Dtos;
using Mindzyxs_API.Repositories;
using Mindzyxs_API.Utilities;

namespace Mindzyxs_API.Services;

// HU4-PB1: verificar cuenta y reenviar código.
public class VerificationService(
    IUserRepository users,
    ICatalogRepository catalogs,
    ICodigoRepository codes,
    IEmailSender email,
    IOptions<RegistroOptions> registroOptions)
{
    public async Task<OperationResult<string>> VerifyAsync(string? correo, string? codigo, CancellationToken ct = default)
    {
        var c = ContactHelper.Normalize(correo);
        var user = await users.FindByCorreoAsync(c, ct);
        if (user is null) return OperationResult<string>.Fail("No se encontró un registro con ese correo.");

        var ultimo = await codes.UltimoAsync(user.IdUsuario, VerificationTypes.Registro, ct);
        if (ultimo is null) return OperationResult<string>.Fail("Código incorrecto. Verifica e intenta nuevamente");
        if (DateTimeProvider.UtcNow() > ultimo.ExpiraEn)
            return OperationResult<string>.Fail("El código expiró. Reenvía el código");
        if (!string.Equals(ultimo.Codigo, (codigo ?? "").Trim(), StringComparison.Ordinal))
            return OperationResult<string>.Fail("Código incorrecto. Verifica e intenta nuevamente");

        await codes.ConsumirAsync(ultimo.IdCodigo, ct);

        var esPsicologo = await EsPsicologoAsync(user.IdRol, ct);
        if (esPsicologo)
        {
            var idPendAprob = await catalogs.GetEstadoIdAsync(AccountStates.PendienteAprobacion, ct);
            await users.SetEstadoCuentaAsync(user.IdUsuario, idPendAprob, ct);
            return OperationResult<string>.Ok("PendienteAprobacion", "Registro exitoso. Tu cuenta está pendiente de aprobación por el administrador");
        }

        var idActiva = await catalogs.GetEstadoIdAsync(AccountStates.Activa, ct);
        await users.SetEstadoCuentaAsync(user.IdUsuario, idActiva, ct);
        return OperationResult<string>.Ok("Activa", "Registro exitoso. Ya puedes iniciar sesión");
    }

    public async Task<OperationResult> ResendAsync(string? correo, CancellationToken ct = default)
    {
        var c = ContactHelper.Normalize(correo);
        var user = await users.FindByCorreoAsync(c, ct);
        if (user is null) return OperationResult.Fail("No se encontró un registro con ese correo.");

        var codigo = CodeHelper.NewCode6();
        var expira = DateTimeProvider.UtcNow().AddMinutes(registroOptions.Value.CodigoExpiraMinutos);
        await codes.CrearAsync(user.IdUsuario, codigo, VerificationTypes.Registro, expira, ct);
        await email.SendAsync(user.Correo, "Mindzyxs - Nuevo código de verificación",
            EmailTemplates.VerificationCode(user.Nombre, codigo), ct);
        return OperationResult.Ok("Código reenviado");
    }

    private async Task<bool> EsPsicologoAsync(int idRol, CancellationToken ct)
    {
        try
        {
            return idRol == await catalogs.GetRolIdAsync(AppRoles.Psicologo, ct);
        }
        catch
        {
            return false;
        }
    }
}
