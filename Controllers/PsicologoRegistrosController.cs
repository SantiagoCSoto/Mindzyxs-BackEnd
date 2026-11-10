using Microsoft.AspNetCore.Mvc;
using Mindzyxs_API.Dtos;
using Mindzyxs_API.Services;

namespace Mindzyxs_API.Controllers;

/// <summary>
/// Registros de pacientes autorizados (PB-18 HU1-PB18): el psicólogo consulta
/// el historial de quienes le otorgaron acceso. Solo psicólogo activo y aprobado.
/// </summary>
[ApiController]
[Route("api/psicologo")]
public class PsicologoRegistrosController(PsicologoRegistrosService registros) : ControllerBase
{
    /// <summary>Pacientes autorizados con búsqueda (nombre, id, estado).</summary>
    /// <remarks>
    /// Filtros opcionales: `q` (nombre/apellido/correo). Vacía → el front muestra
    /// `No tienes pacientes autorizados en este momento`.
    ///
    /// Respuestas:
    /// - `200` — `[ { "id", "nombre" } ]`
    /// - `401` — sin sesión.
    /// - `403` — sin rol/autorización.
    /// </remarks>
    /// <response code="200">Pacientes autorizados.</response>
    /// <response code="401">Sesión requerida.</response>
    /// <response code="403">Solo psicólogos.</response>
    [HttpGet("pacientes")]
    [ProducesResponseType(typeof(List<ProgresoPacienteDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ErrorResult), StatusCodes.Status403Forbidden)]
    public async Task<IActionResult> Pacientes([FromQuery] string? q, CancellationToken ct)
    {
        try
        {
            var ip = HttpContext.Connection.RemoteIpAddress?.ToString();
            return Ok(await registros.PacientesAsync(ReadOptionalBearerToken(), q, ip, ct));
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

    /// <summary>Registros del paciente (emocionales + cuestionario, fecha desc).</summary>
    /// <remarks>
    /// Filtros opcionales: `desde`/`hasta` (solo el rango). 403 mensaje HU si no autorizado.
    ///
    /// Respuestas:
    /// - `200` — lista de registros.
    /// - `400` — rango inválido.
    /// - `401` — sin sesión.
    /// - `403` — `No tienes autorización para ver los registros de este paciente.`
    /// </remarks>
    /// <response code="200">Registros del paciente.</response>
    /// <response code="400">Rango inválido.</response>
    /// <response code="401">Sesión requerida.</response>
    /// <response code="403">Paciente no autorizado.</response>
    [HttpGet("pacientes/{idPaciente:int}/registros")]
    [ProducesResponseType(typeof(List<HistorialItemDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ErrorResult), StatusCodes.Status403Forbidden)]
    public async Task<IActionResult> Registros(int idPaciente, [FromQuery] DateTime? desde, [FromQuery] DateTime? hasta, CancellationToken ct)
    {
        try
        {
            var ip = HttpContext.Connection.RemoteIpAddress?.ToString();
            return Ok(await registros.RegistrosAsync(ReadOptionalBearerToken(), idPaciente, desde, hasta, ip, ct));
        }
        catch (UnauthorizedAccessException ex)
        {
            return Unauthorized(new { error = ex.Message });
        }
        catch (InvalidOperationException ex) when (ex.Message == "La fecha \"Desde\" no puede ser posterior a \"Hasta\".")
        {
            return BadRequest(new { error = ex.Message });
        }
        catch (InvalidOperationException ex)
        {
            return StatusCode(StatusCodes.Status403Forbidden, new { error = ex.Message });
        }
    }

    /// <summary>Detalle completo de un registro (fecha/hora, estado, notas, resultado).</summary>
    /// <remarks>
    /// Respuestas:
    /// - `200` — detalle con respuestas y observaciones.
    /// - `401` — sin sesión.
    /// - `403` — `No tienes autorización para ver este registro.`
    /// - `404` — `Registro no encontrado.`
    /// </remarks>
    /// <response code="200">Detalle del registro.</response>
    /// <response code="401">Sesión requerida.</response>
    /// <response code="403">Sin autorización.</response>
    /// <response code="404">No existe.</response>
    [HttpGet("registros/{idEvento:int}")]
    [ProducesResponseType(typeof(HistorialDetalleDto), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ErrorResult), StatusCodes.Status403Forbidden)]
    [ProducesResponseType(typeof(ErrorResult), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Detalle(int idEvento, CancellationToken ct)
    {
        try
        {
            var ip = HttpContext.Connection.RemoteIpAddress?.ToString();
            return Ok(await registros.DetalleAsync(ReadOptionalBearerToken(), idEvento, ip, ct));
        }
        catch (UnauthorizedAccessException ex)
        {
            return Unauthorized(new { error = ex.Message });
        }
        catch (InvalidOperationException ex) when (ex.Message == "Registro no encontrado.")
        {
            return NotFound(new { error = ex.Message });
        }
        catch (InvalidOperationException ex)
        {
            return StatusCode(StatusCodes.Status403Forbidden, new { error = ex.Message });
        }
    }

    /// <summary>Lee opcionalmente el token de sesión.</summary>
    private string? ReadOptionalBearerToken()
    {
        if (!Request.Headers.TryGetValue("Authorization", out var v)) return null;
        var parts = v.ToString().Trim().Split(' ', StringSplitOptions.RemoveEmptyEntries);
        var token = parts.Length == 1 ? parts[0] : parts[^1];
        return token.Trim().Trim('"').Trim('\'');
    }
}
