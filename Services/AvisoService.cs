using Mindzyxs_API.Dtos;
using Mindzyxs_API.Repositories;
using Mindzyxs_API.Utilities;

namespace Mindzyxs_API.Services;

public class AvisoService(
    ISesionRepository sesiones,
    IAuthLogRepository authLogs)
{
    // PB-04 HU1-PB4: texto completo del Mensaje aclaratorio (no diagnóstico clínico).
    public Task<AvisoDto> GetAvisoAsync(CancellationToken ct = default)
    {
        var aviso = new AvisoDto
        {
            Titulo = "Mensaje aclaratorio",
            Texto = """
                Mindzyx es una plataforma diseñada exclusivamente para el monitoreo y apoyo del bienestar emocional.
                Este sistema no realiza diagnósticos clínicos y no sustituye la atención de un profesional de la salud mental.

                Ante un riesgo alto o una situación de urgencia, busca ayuda profesional o utiliza las líneas de ayuda disponibles en tu país (por ejemplo, servicios de emergencia o líneas de crisis locales).

                Mindzyx es solo una herramienta de apoyo y monitoreo emocional y no reemplaza la atención profesional.
                """
        };
        return Task.FromResult(aviso);
    }

    // PB-04 HU1-PB4: registra la visualización del mensaje (o su error de carga) en dbo.auth.
    // Si viene un token Bearer válido, se asocia al usuario (actorId); si no, null = invitado/anónimo.
    public async Task RegistrarVisualizacionAsync(string? tokenRaw, string? origen, string? resultado, string? ip, CancellationToken ct = default)
    {
        var actorId = await ResolveActorIdAsync(tokenRaw, ct);
        var esError = string.Equals(resultado, "error", StringComparison.OrdinalIgnoreCase);
        var accion = esError ? LogAcciones.AvisoAclaratorioError : LogAcciones.AvisoAclaratorio;
        var detalle = esError
            ? $"No fue posible cargar el mensaje aclaratorio. Intente nuevamente (origen: {OrigenToDisplay(origen)})"
            : $"{RolOrInvitado(actorId)} visualizó el mensaje aclaratorio (origen: {OrigenToDisplay(origen)})";

        await authLogs.LogAsync(actorId, actorId, accion, detalle, ip, ct);
    }

    private async Task<int?> ResolveActorIdAsync(string? tokenRaw, CancellationToken ct)
    {
        if (string.IsNullOrWhiteSpace(tokenRaw)) return null;
        try
        {
            var hash = TokenHelper.Hash(tokenRaw.Trim());
            var sesion = await sesiones.FindByTokenHashAsync(hash, ct);
            return sesion?.IdUsuario;
        }
        catch
        {
            return null;
        }
    }

    private string RolOrInvitado(int? actorId) => actorId is null ? "Invitado" : $"Usuario:{actorId}";

    private static string OrigenToDisplay(string? origen) => string.IsNullOrWhiteSpace(origen) ? "no especificado" : origen.Trim();
}