using Microsoft.AspNetCore.Mvc;
using Mindzyxs_API.Dtos;
using Mindzyxs_API.Services;

namespace Mindzyxs_API.Controllers;

/// <summary>
/// Historial emocional del paciente (PB-13 HU1-PB13): lista ordenada por fecha
/// (recientes primero) con filtro por rango, y detalle con nota propia,
/// respuestas del cuestionario y apoyo del psicólogo. Solo el dueño ve lo suyo.
/// </summary>
[ApiController]
[Route("api/[controller]")]
public class HistorialController(HistorialService historial) : ControllerBase
{
    /// <summary>Devuelve mi historial (recientes primero), opcionalmente filtrado por fechas.</summary>
    /// <remarks>
    /// PB-13 HU1-PB13. Requiere sesión de paciente. `desde`/`hasta` formato `aaaa-MM-dd`.
    /// Vacío (`[]`) → el front muestra "Aún no tienes registros emocionales" con
    /// el botón Registrar estado emocional. Guarda `HISTORIAL_CONSULTA` en `dbo.auth`.
    ///
    /// Respuestas:
    /// - `200` — `[{ "id": ..., "fecha": ..., "tipo": "Cuestionario|RegistroEmocional", "puntaje": ..., "nivel": ..., "cuestionario": ..., "resumen": ... }]`
    /// - `401` — sin sesión.
    /// </remarks>
    /// <response code="200">Historial propio.</response>
    /// <response code="401">Sesión requerida.</response>
    [HttpGet]
    [ProducesResponseType(typeof(List<HistorialItemDto>), StatusCodes.Status200OK)]
    public async Task<IActionResult> Mio([FromQuery] DateTime? desde, [FromQuery] DateTime? hasta, CancellationToken ct)
    {
        try
        {
            var ip = HttpContext.Connection.RemoteIpAddress?.ToString();
            return Ok(await historial.ObtenerAsync(ReadOptionalBearerToken(), desde, hasta, ip, ct));
        }
        catch (UnauthorizedAccessException ex)
        {
            return Unauthorized(new { error = ex.Message });
        }
    }

    /// <summary>Devuelve el detalle de un registro (nota, respuestas y apoyo).</summary>
    /// <remarks>
    /// PB-13 HU1-PB13. Requiere sesión; solo el dueño. Incluye "Cómo me sentí"
    /// (nota), respuestas con texto de pregunta y "Apoyo de tu psicólogo".
    ///
    /// Respuestas:
    /// - `200` — detalle completo.
    /// - `401` — sin sesión.
    /// - `404` — `{ "error": "Registro no encontrado." }`
    /// </remarks>
    /// <response code="200">Detalle del registro.</response>
    /// <response code="401">Sesión requerida.</response>
    /// <response code="404">No existe o no es mío.</response>
    [HttpGet("{id:int}")]
    [ProducesResponseType(typeof(HistorialDetalleDto), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ErrorResult), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Detalle([FromRoute] int id, CancellationToken ct)
    {
        try
        {
            var ip = HttpContext.Connection.RemoteIpAddress?.ToString();
            return Ok(await historial.ObtenerDetalleAsync(ReadOptionalBearerToken(), id, ip, ct));
        }
        catch (UnauthorizedAccessException ex)
        {
            return Unauthorized(new { error = ex.Message });
        }
        catch (InvalidOperationException ex)
        {
            return NotFound(new { error = ex.Message });
        }
    }

    /// <summary>Lee el token de sesión del header.</summary>
    private string? ReadOptionalBearerToken()
    {
        if (!Request.Headers.TryGetValue("Authorization", out var v)) return null;
        var parts = v.ToString().Trim().Split(' ', StringSplitOptions.RemoveEmptyEntries);
        var token = parts.Length == 1 ? parts[0] : parts[^1];
        return token.Trim().Trim('"').Trim('\'');
    }
}
