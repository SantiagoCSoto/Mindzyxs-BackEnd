using Mindzyxs_API.Dtos;
using Mindzyxs_API.Repositories;
using Mindzyxs_API.Utilities;

namespace Mindzyxs_API.Services;

public class InvitadoService(
    ISesionRepository sesiones,
    IAuthLogRepository authLogs)
{
    // PB-06 HU1-PB6: configuración del Panel de invitado. Constante como "configuración"
    // del sistema (la HU menciona "si está configurado como obligatorio" para el aviso).
    public Task<InvitadoPanelDto> ObtenerPanelAsync(CancellationToken ct = default)
    {
        var panel = new InvitadoPanelDto
        {
            AvisoObligatorio = true,
            Disponibles =
            [
                new InvitadoOpcionDto { Codigo = "cuestionario", Titulo = "Responder cuestionario", Descripcion = "Cuestionario de bienestar emocional" },
                new InvitadoOpcionDto { Codigo = "recomendaciones", Titulo = "Ver recomendaciones básicas", Descripcion = "Consejos generales de cuidado" },
                new InvitadoOpcionDto { Codigo = "lineas-ayuda", Titulo = "Líneas de ayuda", Descripcion = "Contactos de emergencia y apoyo" }
            ],
            Exclusivas =
            [
                new InvitadoOpcionDto { Codigo = "historial", Titulo = "Historial emocional", Descripcion = "Registro de tus estados" },
                new InvitadoOpcionDto { Codigo = "graficos", Titulo = "Gráficos de progreso", Descripcion = "Evolución por tiempo" },
                new InvitadoOpcionDto { Codigo = "recordatorios", Titulo = "Recordatorios", Descripcion = "Avisos programados" },
                new InvitadoOpcionDto { Codigo = "psicologo", Titulo = "Autorización de psicólogo", Descripcion = "Vincular un profesional" }
            ]
        };
        return Task.FromResult(panel);
    }

    // PB-06 HU1-PB6: registra el acceso al modo invitado (o su error) en dbo.auth.
    // El invitado no tiene cuenta: actor/target nulos. Si viniera un token Bearer
    // válido (caso límite), se asocia al usuario para trazabilidad.
    public async Task RegistrarAccesoAsync(string? tokenRaw, string? origen, string? resultado, string? ip, CancellationToken ct = default)
    {
        var actorId = await ResolveActorIdAsync(tokenRaw, ct);
        var esError = string.Equals(resultado, "error", StringComparison.OrdinalIgnoreCase);
        var accion = esError ? LogAcciones.InvitadoAccesoError : LogAcciones.InvitadoAcceso;
        var detalle = esError
            ? $"No fue posible acceder en modo invitado. Intente nuevamente (origen: {OrigenToDisplay(origen)})"
            : $"{RolOrInvitado(actorId)} entró en modo invitado (origen: {OrigenToDisplay(origen)})";

        await authLogs.LogAsync(actorId, actorId, accion, detalle, ip, ct);
    }

    // PB-06 HU1-PB6: registra el intento bloqueado de usar una función exclusiva.
    public async Task RegistrarBloqueoAsync(string? tokenRaw, string? funcion, string? ip, CancellationToken ct = default)
    {
        var actorId = await ResolveActorIdAsync(tokenRaw, ct);
        var detalle = $"Intento bloqueado para invitado: {FuncionToDisplay(funcion)} — función exclusiva de usuarios registrados";
        await authLogs.LogAsync(actorId, actorId, LogAcciones.InvitadoBloqueo, detalle, ip, ct);
    }

    // PB-06 HU1-PB6: registra la salida del modo invitado (vuelta a la Landing).
    public async Task RegistrarSalidaAsync(string? tokenRaw, string? origen, string? ip, CancellationToken ct = default)
    {
        var actorId = await ResolveActorIdAsync(tokenRaw, ct);
        var detalle = $"{RolOrInvitado(actorId)} salió del modo invitado (origen: {OrigenToDisplay(origen)})";
        await authLogs.LogAsync(actorId, actorId, LogAcciones.InvitadoSalida, detalle, ip, ct);
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

    private static string FuncionToDisplay(string? funcion) => string.IsNullOrWhiteSpace(funcion) ? "sin especificar" : funcion.Trim();
}