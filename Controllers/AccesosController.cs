using Microsoft.AspNetCore.Mvc;
using Mindzyxs_API.Dtos;
using Mindzyxs_API.Services;

namespace Mindzyxs_API.Controllers;

/// <summary>
/// Gestión de accesos del paciente (PB-17 HU1-PB17): autoriza o revoca el acceso
/// de psicólogos a su información emocional. Solo pacientes con sesión.
/// </summary>
[ApiController]
[Route("api/accesos")]
public class AccesosController(AccesosService accesos) : ControllerBase
{
    /// <summary>Psicólogos aprobados+activos con estado de autorización del paciente.</summary>
    /// <remarks>
    /// Filtros opcionales: `q` (nombre/apellido). Vacía → el front muestra
    /// `No hay psicólogos disponibles para autorizar en este momento`.
    ///
    /// Respuestas:
    /// - `200` — `[ { "idPsicologo", "nombre", "especialidades": [], "autorizado": true } ]`
    /// - `401` — sin sesión.
    /// - `403` — sin rol Paciente.
    /// </remarks>
    /// <response code="200">Accesos del paciente.</response>
    /// <response code="401">Sesión requerida.</response>
    /// <response code="403">Solo pacientes.</response>
    [HttpGet]
    [ProducesResponseType(typeof(List<AccesoPsicologoDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ErrorResult), StatusCodes.Status403Forbidden)]
    public async Task<IActionResult> MisAccesos([FromQuery] string? q, CancellationToken ct)
    {
        try
        {
            var ip = HttpContext.Connection.RemoteIpAddress?.ToString();
            return Ok(await accesos.MisAccesosAsync(ReadOptionalBearerToken(), q, ip, ct));
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

    /// <summary>Autoriza a un psicólogo aprobado y activo.</summary>
    /// <remarks>
    /// Respuestas: `200` — `{ "mensaje": "Acceso autorizado correctamente" }`;
    /// `401` sin sesión; `403` sin rol; `404` psicólogo no válido.
    /// </remarks>
    /// <response code="200">Autorizado.</response>
    /// <response code="401">Sesión requerida.</response>
    /// <response code="403">Solo pacientes.</response>
    /// <response code="404">Psicólogo no válido.</response>
    [HttpPost("{idPsicologo:int}/autorizar")]
    [ProducesResponseType(typeof(MessageResult), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ErrorResult), StatusCodes.Status403Forbidden)]
    [ProducesResponseType(typeof(ErrorResult), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Autorizar(int idPsicologo, CancellationToken ct)
    {
        try
        {
            var ip = HttpContext.Connection.RemoteIpAddress?.ToString();
            return Ok(new { mensaje = await accesos.AutorizarAsync(ReadOptionalBearerToken(), idPsicologo, ip, ct) });
        }
        catch (UnauthorizedAccessException ex)
        {
            return Unauthorized(new { error = ex.Message });
        }
        catch (InvalidOperationException ex) when (ex.Message == "Psicólogo no encontrado." || ex.Message == "Psicólogo no válido.")
        {
            return NotFound(new { error = ex.Message });
        }
        catch (InvalidOperationException ex)
        {
            return StatusCode(StatusCodes.Status403Forbidden, new { error = ex.Message });
        }
    }

    /// <summary>Revoca el acceso de un psicólogo.</summary>
    /// <remarks>
    /// Respuestas: `200` — `{ "mensaje": "Acceso revocado correctamente" }`;
    /// `401` sin sesión; `403` sin rol; `404` psicólogo inexistente.
    /// </remarks>
    /// <response code="200">Revocado.</response>
    /// <response code="401">Sesión requerida.</response>
    /// <response code="403">Solo pacientes.</response>
    /// <response code="404">Psicólogo inexistente.</response>
    [HttpPost("{idPsicologo:int}/revocar")]
    [ProducesResponseType(typeof(MessageResult), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ErrorResult), StatusCodes.Status403Forbidden)]
    [ProducesResponseType(typeof(ErrorResult), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Revocar(int idPsicologo, CancellationToken ct)
    {
        try
        {
            var ip = HttpContext.Connection.RemoteIpAddress?.ToString();
            return Ok(new { mensaje = await accesos.RevocarAsync(ReadOptionalBearerToken(), idPsicologo, ip, ct) });
        }
        catch (UnauthorizedAccessException ex)
        {
            return Unauthorized(new { error = ex.Message });
        }
        catch (InvalidOperationException ex) when (ex.Message == "Psicólogo no encontrado.")
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
