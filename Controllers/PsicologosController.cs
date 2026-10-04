using Microsoft.AspNetCore.Mvc;
using Mindzyxs_API.Dtos;
using Mindzyxs_API.Services;

namespace Mindzyxs_API.Controllers;

/// <summary>
/// Directorio de psicólogos para pacientes: nombre, especialidades y pacientes
/// asignados, para elegir cuál cuadra mejor. Solo pacientes con sesión.
/// </summary>
[ApiController]
[Route("api/psicologos")]
public class PsicologosController(PsicologoService psicologos) : ControllerBase
{
    /// <summary>Devuelve el directorio de psicólogos (activas y aprobadas).</summary>
    /// <remarks>
    /// Solo pacientes con sesión. Filtros opcionales: `q` (nombre/apellido) e
    /// `idEspecialidad` (checklist de especialidades). Cada tarjeta trae nombre,
    /// especialidades (vacía si no registró ninguna) y pacientes asignados (autorizados).
    ///
    /// Respuestas:
    /// - `200` — `[ { "id", "nombre", "especialidades": [], "pacientesAsignados": 0 } ]`
    /// - `401` — sin sesión.
    /// - `403` — sin rol Paciente.
    /// </remarks>
    /// <response code="200">Directorio de psicólogos.</response>
    /// <response code="401">Sesión requerida.</response>
    /// <response code="403">Solo pacientes.</response>
    [HttpGet]
    [ProducesResponseType(typeof(List<PsicologoDirectorioDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ErrorResult), StatusCodes.Status403Forbidden)]
    public async Task<IActionResult> Directorio([FromQuery] string? q, [FromQuery] int? idEspecialidad, CancellationToken ct)
    {
        try
        {
            var ip = HttpContext.Connection.RemoteIpAddress?.ToString();
            return Ok(await psicologos.DirectorioAsync(ReadOptionalBearerToken(), q, idEspecialidad, ip, ct));
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

    /// <summary>Lee opcionalmente el token de sesión.</summary>
    private string? ReadOptionalBearerToken()
    {
        if (!Request.Headers.TryGetValue("Authorization", out var v)) return null;
        var parts = v.ToString().Trim().Split(' ', StringSplitOptions.RemoveEmptyEntries);
        var token = parts.Length == 1 ? parts[0] : parts[^1];
        return token.Trim().Trim('"').Trim('\'');
    }
}
