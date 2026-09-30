using Microsoft.AspNetCore.Mvc;
using Mindzyxs_API.Dtos;
using Mindzyxs_API.Services;

namespace Mindzyxs_API.Controllers;

/// <summary>
/// Historial emocional del paciente (PB-13 HU1-PB13): lista ordenada por fecha
/// (recientes primero) con filtro por rango, y detalle con nota propia,
/// respuestas del cuestionario y apoyo del psicólogo. Solo el dueño ve lo suyo.
/// Registro del estado emocional (PB-12 HU1-PB12): guarda escala 1-5 + nota
/// opcional con fecha/hora automática, actualizando el historial (log en dbo.auth).
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
    [ProducesResponseType(typeof(ErrorResult), StatusCodes.Status400BadRequest)]
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
        catch (InvalidOperationException ex)
        {
            return BadRequest(new { error = ex.Message });
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

    /// <summary>Guarda el registro del estado emocional del paciente (escala 1-5 + nota opcional).</summary>
    /// <remarks>
    /// PB-12 HU1-PB12. Requiere sesión de paciente. La fecha y hora se asignan
    /// automáticamente al guardar; el registro queda visible en el historial
    /// (`GET /api/historial`) y se registra `REGISTRO_EMOCIONAL_CREADO` en `dbo.auth`.
    ///
    /// Respuestas:
    /// - `200` — `{ "mensaje": "Registro guardado correctamente", "idEvento": ..., "fecha": ..., "puntaje": ..., "nivel": ..., "nota": ... }`
    /// - `400` — `{ "error": "Debes seleccionar un estado emocional para guardar" }` (falta o inválida la escala)
    /// - `400` — `{ "error": "Este campo es obligatorio" }` (body inválido/vacío)
    /// - `401` — sin sesión.
    /// </remarks>
    /// <response code="200">Registro guardado.</response>
    /// <response code="400">Falta el estado emocional o body inválido.</response>
    /// <response code="401">Sesión requerida.</response>
    [HttpPost]
    [ProducesResponseType(typeof(RegistroEmocionalCreadoResult), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ErrorResult), StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> Registrar([FromBody] RegistrarEstadoEmocionalVm vm, CancellationToken ct)
    {
        if (!ModelState.IsValid)
            return BadRequest(new { error = "Este campo es obligatorio" });

        var ip = HttpContext.Connection.RemoteIpAddress?.ToString();
        try
        {
            var r = await historial.RegistrarEstadoEmocionalAsync(ReadOptionalBearerToken(), vm, ip, ct);
            return Ok(new
            {
                mensaje = "Registro guardado correctamente",
                idEvento = r.IdEvento,
                fecha = r.Fecha,
                puntaje = r.Puntaje,
                nivel = r.Nivel,
                nota = r.Nota
            });
        }
        catch (UnauthorizedAccessException ex)
        {
            return Unauthorized(new { error = ex.Message });
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(new { error = ex.Message });
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
