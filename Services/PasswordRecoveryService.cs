using Microsoft.Extensions.Options;
using Mindzyxs_API.Dtos;
using Mindzyxs_API.Repositories;
using Mindzyxs_API.Utilities;

namespace Mindzyxs_API.Services;

// PB-05 HU1-PB5 + HU2-PB5: recuperar contraseña.
// Mismo patrón que VerificationService: usa dbo.codigos_verificacion (tipo "Recuperar"),
// política de contraseña de SecurityHelper, correo de EmailTemplates y log en dbo.auth.
public class PasswordRecoveryService(
    IUserRepository users,
    ICodigoRepository codes,
    ISesionRepository sesiones,
    IAuthLogRepository authLogs,
    NotificationService notificaciones,
    IOptions<RegistroOptions> registroOptions,
    ILogger<PasswordRecoveryService> logger)
{
    // HU1-PB5: solicitar código. Si el correo existe genera código "Recuperar" y lo envía.
    // Si NO existe devuelve un mensaje genérico de seguridad (no confirma si la cuenta existe).
    public async Task<OperationResult> ForgotAsync(string? correoRaw, CancellationToken ct = default)
    {
        var correo = ContactHelper.Normalize(correoRaw);
        if (string.IsNullOrWhiteSpace(correo))
            return OperationResult.Fail("Este campo es obligatorio");
        if (!ContactHelper.IsEmail(correo))
            return OperationResult.Fail("Ingresa un correo válido");

        var user = await users.FindByCorreoAsync(correo, ct);
        if (user is null)
            return OperationResult.Ok("Si el correo está registrado, recibirás un código");

        var codigo = CodeHelper.NewCode6();
        var expira = DateTimeProvider.UtcNow().AddMinutes(registroOptions.Value.CodigoExpiraMinutos);
        await codes.CrearAsync(user.IdUsuario, codigo, VerificationTypes.Recuperar, expira, ct);

        try
        {
            // PB-30: respeta la config del admin (PASSWORD_RECOVERY).
            await notificaciones.EnviarCorreoAsync(TiposNotificacion.PasswordRecovery, user.Correo,
                new Dictionary<string, string> { ["nombre"] = user.Nombre, ["codigo"] = codigo },
                "Mindzyxs - Código de recuperación",
                v => EmailTemplates.RecoveryCode(v["nombre"], v["codigo"]), user.IdUsuario, null, ct);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "No se pudo enviar el código de recuperación");
        }

        // Anti-enumeración: mismo mensaje exista o no la cuenta.
        return OperationResult.Ok("Si el correo está registrado, recibirás un código");
    }

    // HU2-PB5: validar el código antes de mostrar la pantalla Nueva contraseña.
    public async Task<OperationResult> VerifyCodeAsync(string? correoRaw, string? codigo, CancellationToken ct = default)
    {
        var correo = ContactHelper.Normalize(correoRaw);
        var user = await users.FindByCorreoAsync(correo, ct);
        if (user is null)
            return OperationResult.Fail("Código incorrecto. Verifica e intenta nuevamente");

        var ultimo = await codes.UltimoAsync(user.IdUsuario, VerificationTypes.Recuperar, ct);
        if (ultimo is null)
            return OperationResult.Fail("Código incorrecto. Verifica e intenta nuevamente");
        if (DateTimeProvider.UtcNow() > ultimo.ExpiraEn)
            return OperationResult.Fail("El código expiró. Reenvía el código");
        if (!string.Equals(ultimo.Codigo, (codigo ?? "").Trim(), StringComparison.Ordinal))
            return OperationResult.Fail("Código incorrecto. Verifica e intenta nuevamente");

        return OperationResult.Ok("Código verificado. Define tu nueva contraseña");
    }

    // HU2-PB5: revalida el código, valida la política, actualiza password_hash,
    // consume el código y registra PASSWORD_RESET en dbo.auth.
    public async Task<OperationResult> ResetPasswordAsync(string? correoRaw, string? codigo, string? password, string? ip, CancellationToken ct = default)
    {
        var correo = ContactHelper.Normalize(correoRaw);
        var user = await users.FindByCorreoAsync(correo, ct);
        if (user is null)
            return OperationResult.Fail("Código incorrecto. Verifica e intenta nuevamente");

        var ultimo = await codes.UltimoAsync(user.IdUsuario, VerificationTypes.Recuperar, ct);
        if (ultimo is null)
            return OperationResult.Fail("Código incorrecto. Verifica e intenta nuevamente");
        if (DateTimeProvider.UtcNow() > ultimo.ExpiraEn)
            return OperationResult.Fail("El código expiró. Reenvía el código");
        if (!string.Equals(ultimo.Codigo, (codigo ?? "").Trim(), StringComparison.Ordinal))
            return OperationResult.Fail("Código incorrecto. Verifica e intenta nuevamente");

        if (string.IsNullOrWhiteSpace(password) || !PasswordHelper.IsStrongEnough(password))
            return OperationResult.Fail("La contraseña debe tener mínimo 6 caracteres e incluir al menos una letra y un número");

        try
        {
            await users.SetPasswordAsync(user.IdUsuario, PasswordHelper.Hash(password), ct);
            await codes.ConsumirAsync(ultimo.IdCodigo, ct);
            // Seguridad: al cambiar la contraseña se revocan todas las sesiones activas.
            await sesiones.DeleteByUsuarioAsync(user.IdUsuario, ct);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "No se pudo actualizar la contraseña");
            return OperationResult.Fail("No fue posible actualizar la contraseña. Intenta nuevamente");
        }

        try
        {
            await authLogs.LogAsync(user.IdUsuario, user.IdUsuario, LogAcciones.PasswordReset,
                "Contraseña restablecida", ip, ct);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "No se pudo registrar {Accion}", LogAcciones.PasswordReset);
        }

        return OperationResult.Ok("Contraseña actualizada exitosamente");
    }

    // HU2-PB5: Reenviar código. Genérico si el correo no existe para no revelar cuentas.
    public async Task<OperationResult> ResendAsync(string? correoRaw, CancellationToken ct = default)
    {
        var correo = ContactHelper.Normalize(correoRaw);
        var user = await users.FindByCorreoAsync(correo, ct);
        if (user is null)
            return OperationResult.Ok("Código reenviado");

        var codigo = CodeHelper.NewCode6();
        var expira = DateTimeProvider.UtcNow().AddMinutes(registroOptions.Value.CodigoExpiraMinutos);
        await codes.CrearAsync(user.IdUsuario, codigo, VerificationTypes.Recuperar, expira, ct);

        try
        {
            // PB-30: respeta la config del admin (PASSWORD_RECOVERY).
            await notificaciones.EnviarCorreoAsync(TiposNotificacion.PasswordRecovery, user.Correo,
                new Dictionary<string, string> { ["nombre"] = user.Nombre, ["codigo"] = codigo },
                "Mindzyxs - Nuevo código de recuperación",
                v => EmailTemplates.RecoveryCode(v["nombre"], v["codigo"]), user.IdUsuario, null, ct);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "No se pudo enviar el nuevo código a {Correo}", user.Correo);
        }

        return OperationResult.Ok("Código reenviado");
    }
}