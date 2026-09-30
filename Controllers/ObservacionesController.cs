using Microsoft.AspNetCore.Mvc;
using Mindzyxs_API.Dtos;
using Mindzyxs_API.Services;

namespace Mindzyxs_API.Controllers;

/// <summary>
/// Observaciones del psicólogo (PB-19 HU1-PB19): registra observaciones sobre
/// pacientes autorizados, con firma y fecha/hora automáticas.
/// Solo psicólogo activo y aprobado.
/// </summary>
[ApiController]
[Route("api/observaciones")]
public class ObservacionesController(ObservacionesService observaciones) : ControllerBase
{
    /// <summary>Guarda una observación sobre un paciente autorizado.</summary>
    /// <remarks>
    /// Ejemplo:
    ///
    ///     POST /api/observaciones
    ///     { "idPaciente": 12, "idEvento": 1013, "contenido": "El paciente muestra mejoría…" }
    ///
    /// `idEvento` es opcional (el front lo preselecciona si la observación nace de un registro).
    ///
    /// Respuestas:
    /// - `200` — `{ "mensaje": "Observación guardada correctamente" }`
    /// - `400` — `La observación es obligatoria.`
    /// - `401` — sin sesión.
    /// - `403` — `No tienes autorización para agregar observaciones a este paciente.` / sin rol.
    /// - `404` — `Paciente no encontrado.` / `Registro no encontrado.`
    /// </remarks>
    /// <response code="200">Observación guardada.</response>
    /// <response code="400">Contenido vacío.</response>
    /// <response code="401">Sesión requerida.</response>
    /// <response code="403">Paciente no autorizado.</response>
    /// <response code="404">Paciente o registro inexistente.</response>
    [HttpPost]
    [ProducesResponseType(typeof(MessageResult), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ErrorResult), StatusCodes.Status403Forbidden)]
    [ProducesResponseType(typeof(ErrorResult), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Crear([FromBody] ObservacionCrearDto dto, CancellationToken ct)
    {
        try
        {
            var ip = HttpContext.Connection.RemoteIpAddress?.ToString();
            return Ok(new { mensaje = await observaciones.CrearAsync(ReadOptionalBearerToken(), dto, ip, ct) });
        }
        catch (UnauthorizedAccessException ex)
        {
            return Unauthorized(new { error = ex.Message });
        }
        catch (KeyNotFoundException ex)
        {
            return NotFound(new { error = ex.Message });
        }
        catch (InvalidOperationException ex) when (ex.Message == "La observación es obligatoria.")
        {
            return BadRequest(new { error = ex.Message });
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
