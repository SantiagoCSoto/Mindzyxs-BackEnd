using Microsoft.AspNetCore.Mvc;
using Mindzyxs_API.Dtos;
using Mindzyxs_API.Services;

namespace Mindzyxs_API.Controllers;

/// <summary>
/// Recomendaciones básicas de autocuidado del invitado (PB-09 HU1-PB9): lista priorizada
/// por el nivel de riesgo de la sesión y detalle con pasos, duración e instrucciones.
/// Contenido administrable por el administrador (PB-28). Solo autocuidado básico
/// (no tratamiento clínico). Endpoints públicos (el invitado no tiene sesión).
/// </summary>
[ApiController]
[Route("api/recomendaciones")] // plural explícito: coincide con front/.http/README (el [controller] daría singular)
public class RecomendacionController(RecomendacionService recomendaciones) : ControllerBase
{
    /// <summary>Devuelve la lista de recomendaciones, priorizada por nivel de la sesión.</summary>
    /// <remarks>
    /// PB-09 HU1-PB9. Público. `nivel` = Bajo/Medio/Alto calculado en PB-08 (viene de la
    /// sesión/cookie del front); sin nivel, orden estándar. La primera del nivel de la
    /// sesión lleva `prioritaria: true`. Guarda `RECOMENDACION_INVITADO_CONSULTA` en `dbo.auth`.
    ///
    /// Respuestas:
    /// - `200` — `{ "disponible": true, "nivel": "Medio", "recomendaciones": [{ "id": ..., "titulo": ..., "intro": ..., "icono": ..., "nivel": ..., "prioritaria": ... }] }`
    /// - `200` — `{ "disponible": false, "mensaje": "No hay recomendaciones disponibles en este momento" }` (el front muestra el estado vacío con Volver)
    /// </remarks>
    /// <response code="200">Lista de recomendaciones o estado vacío.</response>
    [HttpGet("invitado")]
    [ProducesResponseType(typeof(RecomendacionesInvitadoDto), StatusCodes.Status200OK)]
    public async Task<ActionResult<RecomendacionesInvitadoDto>> Lista([FromQuery] string? nivel, CancellationToken ct)
    {
        var ip = HttpContext.Connection.RemoteIpAddress?.ToString();
        return Ok(await recomendaciones.ObtenerListaAsync(ReadOptionalBearerToken(), nivel, ip, ct));
    }

    /// <summary>Devuelve el detalle de una recomendación (pasos, duración, instrucciones).</summary>
    /// <remarks>
    /// PB-09 HU1-PB9. Público. El front lo invoca con Ver/Abrir. Muestra el mensaje
    /// aclaratorio de no diagnóstico junto al contenido. Guarda consulta en `dbo.auth`.
    ///
    /// Respuestas:
    /// - `200` — `{ "id": ..., "titulo": ..., "intro": ..., "icono": ..., "nivel": ..., "duracion": ..., "pasos": [...], "instrucciones": ..., "avisoObligatorio": true }`
    /// - `404` — `{ "error": "Recomendación no disponible." }`
    /// </remarks>
    /// <response code="200">Detalle de la recomendación.</response>
    /// <response code="404">No existe la recomendación.</response>
    [HttpGet("invitado/{id:int}")]
    [ProducesResponseType(typeof(RecomendacionDetalleDto), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ErrorResult), StatusCodes.Status404NotFound)]
    public async Task<ActionResult<RecomendacionDetalleDto>> Detalle([FromRoute] int id, CancellationToken ct)
    {
        var ip = HttpContext.Connection.RemoteIpAddress?.ToString();
        try
        {
            return Ok(await recomendaciones.ObtenerDetalleAsync(ReadOptionalBearerToken(), id, ip, ct));
        }
        catch (InvalidOperationException ex)
        {
            return NotFound(new { error = ex.Message });
        }
    }

    /// <summary>Lee opcionalmente el token de sesión (no requiere ser válido para el registro).</summary>
    private string? ReadOptionalBearerToken()
    {
        if (!Request.Headers.TryGetValue("Authorization", out var v)) return null;
        var parts = v.ToString().Trim().Split(' ', StringSplitOptions.RemoveEmptyEntries);
        var token = parts.Length == 1 ? parts[0] : parts[^1];
        return token.Trim().Trim('"').Trim('\'');
    }
}
