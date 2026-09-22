using Mindzyxs_API.Dtos;
using Mindzyxs_API.Repositories;
using Mindzyxs_API.Utilities;

namespace Mindzyxs_API.Services;

public class RecomendacionService(
    IRecomendacionRepository recomendaciones,
    ISesionRepository sesiones,
    IAuthLogRepository authLogs)
{
    // PB-09 HU1-PB9: lista de autocuidado básico, priorizada por el nivel de la sesión.
    // Sin nivel -> orden estándar. Sin contenido -> estado vacío. Log de consulta (lista).
    public async Task<RecomendacionesInvitadoDto> ObtenerListaAsync(string? tokenRaw, string? nivel, string? ip, CancellationToken ct = default)
    {
        var actorId = await ResolveActorIdAsync(tokenRaw, ct);
        var n = NormalizarNivel(nivel);
        // Corrección BD: el orden por nivel ya viene del SP (sp_recomendaciones_priorizadas).
        var todas = await recomendaciones.GetPriorizadasAsync(n, ct);
        await authLogs.LogAsync(actorId, actorId, LogAcciones.RecomendacionInvitadoConsulta,
            $"{RolOrInvitado(actorId)} consultó la lista de recomendaciones básicas (nivel de sesión: {NivelToDisplay(nivel)})", ip, ct);

        if (todas.Count == 0)
            return new RecomendacionesInvitadoDto
            {
                Disponible = false,
                Mensaje = "No hay recomendaciones disponibles en este momento"
            };

        var items = todas
            .Select(r => new RecomendacionItemDto
            {
                Id = r.IdRecomendacion,
                Titulo = r.Titulo,
                Intro = r.Intro ?? r.Contenido,
                Icono = r.Icono,
                Nivel = r.NivelRiesgo,
                Prioritaria = false
            }).ToList();

        if (n != null)
        {
            var top = items.FirstOrDefault(i => string.Equals(i.Nivel, n, StringComparison.OrdinalIgnoreCase));
            if (top != null) top.Prioritaria = true;
        }

        return new RecomendacionesInvitadoDto { Disponible = true, Nivel = n, Recomendaciones = items };
    }

    // PB-09 HU1-PB9: detalle con pasos, duración e instrucciones. Log de consulta (detalle).
    // Lanza InvalidOperationException ("Recomendación no disponible.") si no existe.
    public async Task<RecomendacionDetalleDto> ObtenerDetalleAsync(string? tokenRaw, int id, string? ip, CancellationToken ct = default)
    {
        var actorId = await ResolveActorIdAsync(tokenRaw, ct);
        var r = await recomendaciones.FindByIdAsync(id, ct);
        if (r is null) throw new InvalidOperationException("Recomendación no disponible.");

        await authLogs.LogAsync(actorId, actorId, LogAcciones.RecomendacionInvitadoConsulta,
            $"{RolOrInvitado(actorId)} abrió el detalle de la recomendación \"{r.Titulo}\" (id: {r.IdRecomendacion})", ip, ct);

        return new RecomendacionDetalleDto
        {
            Id = r.IdRecomendacion,
            Titulo = r.Titulo,
            Intro = r.Intro ?? r.Contenido,
            Icono = r.Icono,
            Nivel = r.NivelRiesgo,
            Duracion = r.Duracion,
            Pasos = SplitPasos(r.Pasos),
            Instrucciones = r.Instrucciones,
            AvisoObligatorio = true
        };
    }

    // Niveles válidos de sesión: Bajo, Medio, Alto (lo demás = sin priorizar).
    private static string? NormalizarNivel(string? nivel)
    {
        if (string.IsNullOrWhiteSpace(nivel)) return null;
        var n = nivel.Trim();
        if (string.Equals(n, "bajo", StringComparison.OrdinalIgnoreCase)) return "Bajo";
        if (string.Equals(n, "medio", StringComparison.OrdinalIgnoreCase)) return "Medio";
        if (string.Equals(n, "alto", StringComparison.OrdinalIgnoreCase)) return "Alto";
        return null;
    }

    private static List<string> SplitPasos(string? pasos)
        => (pasos ?? "").Split(["\r\n", "\n"], StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries).ToList();

    private static string NivelToDisplay(string? nivel) => NormalizarNivel(nivel) ?? "sin nivel calculado";

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
}
