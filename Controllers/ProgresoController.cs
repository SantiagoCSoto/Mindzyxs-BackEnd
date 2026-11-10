using Microsoft.AspNetCore.Mvc;
using Mindzyxs_API.Dtos;
using Mindzyxs_API.Services;

namespace Mindzyxs_API.Controllers;

/// <summary>
/// Progreso emocional de pacientes autorizados (PB-14 HU1-PB14): lista de
/// pacientes, agregados (promedio, tendencia, semanas, distribución) y serie
/// temporal para gráficos. Solo psicólogo y solo autorizados.
/// </summary>
[ApiController]
[Route("api/[controller]")]
public class ProgresoController(ProgresoService progreso) : ControllerBase
{
    /// <summary>Devuelve mis pacientes autorizados.</summary>
    /// <remarks>
    /// PB-14 HU1-PB14. Requiere sesión de psicólogo. Vacía (`[]`) → el front
    /// muestra "No tienes pacientes autorizados en este momento" con Volver.
    ///
    /// Respuestas:
    /// - `200` — `[{ "id": ..., "nombre": ... }]`
    /// - `401` — sin sesión.
    /// </remarks>
    /// <response code="200">Pacientes autorizados.</response>
    /// <response code="401">Sesión requerida.</response>
    [HttpGet("pacientes")]
    [ProducesResponseType(typeof(List<ProgresoPacienteDto>), StatusCodes.Status200OK)]
    public async Task<IActionResult> Pacientes(CancellationToken ct)
    {
        try
        {
            var ip = HttpContext.Connection.RemoteIpAddress?.ToString();
            return Ok(await progreso.AutorizadosAsync(ReadOptionalBearerToken(), ip, ct));
        }
        catch (UnauthorizedAccessException ex)
        {
            return Unauthorized(new { error = ex.Message });
        }
    }

    /// <summary>Devuelve agregados y serie del paciente en el rango.</summary>
    /// <remarks>
    /// PB-14 HU1-PB14. Requiere sesión de psicólogo autorizado para ese paciente.
    /// `suficientes:false` → el front muestra "No tienes registros suficientes
    /// para generar gráficos" con el botón de ver registros.
    ///
    /// Respuestas:
    /// - `200` — `{ "promedio": ..., "tendencia": "Sube|Baja|Estable", "totalRegistros": ..., "distribucion": {...}, "porSemana": [...], "serie": [...], "suficientes": ... }`
    /// - `401` — sin sesión.
    /// - `403` — `{ "error": "No tienes autorización para ver los registros de este paciente." }`
    /// </remarks>
    /// <response code="200">Progreso calculado.</response>
    /// <response code="401">Sesión requerida.</response>
    /// <response code="403">Paciente no autorizado.</response>
    [HttpGet("{idPaciente:int}")]
    [ProducesResponseType(typeof(ProgresoDto), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ErrorResult), StatusCodes.Status403Forbidden)]
    public async Task<IActionResult> DePaciente([FromRoute] int idPaciente, [FromQuery] DateTime? desde, [FromQuery] DateTime? hasta, CancellationToken ct)
    {
        try
        {
            var ip = HttpContext.Connection.RemoteIpAddress?.ToString();
            return Ok(await progreso.ObtenerAsync(ReadOptionalBearerToken(), idPaciente, desde, hasta, ip, ct));
        }
        catch (UnauthorizedAccessException ex)
        {
            return Unauthorized(new { error = ex.Message });
        }
        catch (InvalidOperationException ex)
        {
            return StatusCode(StatusCodes.Status403Forbidden, new { error = ex.Message });
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
