using Mindzyxs_API.Repositories;
using Mindzyxs_API.Utilities;

namespace Mindzyxs_API.Services;

/// <summary>Tipos de evento del catálogo (fiel a v1 CU31).</summary>
public static class TiposNotificacion
{
    public const string CodeSent = "CODE_SENT";
    public const string PasswordRecovery = "PASSWORD_RECOVERY";
    public const string ApprovalGranted = "APPROVAL_GRANTED";
    public const string ApprovalRejected = "APPROVAL_REJECTED";
    public const string Reminder = "REMINDER";
    public const string RiskHigh = "RISK_HIGH";

    public static readonly string[] Todos = [CodeSent, PasswordRecovery, ApprovalGranted, ApprovalRejected, Reminder, RiskHigh];
}

// PB-30: los envíos consultan la config del admin (habilitado + plantilla con
// variables). Sin fila → comportamiento actual (fallback). Solo existe el canal
// correo (no se envía SMS).
public class NotificationService(
    INotificacionRepository notificaciones,
    IEmailSender email,
    IAuthLogRepository authLogs)
{
    // Envía el correo del evento con su plantilla (o fallback). Devuelve true si envió.
    public async Task<bool> EnviarCorreoAsync(
        string tipo, string destino,
        Dictionary<string, string> vars,
        string fallbackSubject, Func<Dictionary<string, string>, string> fallbackBody,
        int? actorId, string? ip, CancellationToken ct = default)
    {
        var cfg = await notificaciones.FindByTipoAsync(tipo, ct);
        if (cfg is null)
        {
            await email.SendAsync(destino, fallbackSubject, fallbackBody(vars), ct);
            return true;
        }
        if (!cfg.Activo)
        {
            await authLogs.LogAsync(actorId, actorId, LogAcciones.CorreoOmitido,
                $"Notificación {tipo} omitida (desactivada por el admin)", ip, ct);
            return false;
        }
        var asunto = Sustituir(string.IsNullOrWhiteSpace(cfg.Asunto) ? fallbackSubject : cfg.Asunto, vars);
        var cuerpo = Sustituir(string.IsNullOrWhiteSpace(cfg.Cuerpo) ? fallbackBody(vars) : cfg.Cuerpo, vars);
        var canales = (cfg.Canales ?? "correo").Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
            .Select(c => c.ToLowerInvariant()).ToHashSet();
        if (!canales.Contains("correo"))
        {
            await authLogs.LogAsync(actorId, actorId, LogAcciones.CorreoOmitido,
                $"Notificación {tipo} omitida (sin canal correo)", ip, ct);
            return false;
        }
        await email.SendAsync(destino, asunto, cuerpo, ct);
        return true;
    }

    // Sustituye {nombre}, {codigo}, {fecha}, {motivo} (las que vengan en vars).
    public static string Sustituir(string plantilla, Dictionary<string, string> vars)
    {
        var r = plantilla;
        foreach (var (k, v) in vars)
            r = r.Replace("{" + k + "}", v ?? "", StringComparison.Ordinal);
        return r;
    }
}
