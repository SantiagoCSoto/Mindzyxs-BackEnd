using Microsoft.AspNetCore.Mvc;
using Mindzyxs_API.Dtos;
using Mindzyxs_API.Services;

namespace Mindzyxs_API.Controllers;

/// <summary>
/// Perfil profesional del psicólogo (PB-24 HU1-PB24): visualiza y edita su información profesional
/// (especialidades, años de experiencia, descripción, datos de contacto, tarjeta profesional).
/// Solo psicólogo activo y aprobado.
/// </summary>
[ApiController]
[Route("api/psicologo/perfil-profesional")]
public class PsicologoPerfilController(PsicologoService psicologos) : ControllerBase
{
    /// <summary>Obtiene el perfil profesional del psicólogo autenticado.</summary>
    /// <remarks>
    /// PB-24 HU1-PB24. Requiere psicólogo activo y aprobado.
    /// Devuelve la información profesional completa: especialidades, años de experiencia,
    /// descripción profesional, datos de contacto, estado de la tarjeta profesional,
    /// y si tiene archivo de tarjeta subido.
    ///
    /// Respuestas:
    /// - `200` — `{ "id", "nombre", "correo", "añosExperiencia", "descripcionProfesional", "datosContacto", "tarjetaProfesionalVerificada", "especialidades", "tieneTarjetaArchivo" }`
    /// - `401` — sin sesión.
    /// - `403` — no es psicólogo / no está activo / no está aprobado.
    /// </remarks>
    /// <response code="200">Perfil profesional.</response>
    /// <response code="401">Sesión requerida.</response>
    /// <response code="403">Solo psicólogos activos y aprobados.</response>
    [HttpGet]
    [ProducesResponseType(typeof(PerfilProfesionalVm), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ErrorResult), StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(typeof(ErrorResult), StatusCodes.Status403Forbidden)]
    public async Task<IActionResult> Obtener(CancellationToken ct)
    {
        try
        {
            return Ok(await psicologos.ObtenerPerfilProfesionalAsync(ReadOptionalBearerToken(), ct));
        }
        catch (UnauthorizedAccessException ex)
        {
            return Unauthorized(new { error = ex.Message });
        }
        catch (InvalidOperationException ex) when (EsSinPermisos(ex.Message))
        {
            return StatusCode(StatusCodes.Status403Forbidden, new { error = ex.Message });
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(new { error = ex.Message });
        }
    }

    /// <summary>Actualiza el perfil profesional del psicólogo autenticado.</summary>
    /// <remarks>
    /// PB-24 HU1-PB24. Requiere psicólogo activo y aprobado.
    /// Permite actualizar: años de experiencia, descripción profesional, datos de contacto,
    /// estado de verificación de tarjeta profesional, especialidades (checklist) y correo.
    /// Valida:
    /// - Años de experiencia >= 0
    /// - Correo válido y único (si se modifica)
    /// - Especialidades existentes
    /// Guarda `PERFIL_PROFESIONAL_ACTUALIZADO` en `dbo.auth`.
    ///
    /// Respuestas:
    /// - `200` — `{ "mensaje": "Información profesional actualizada correctamente" }`
    /// - `400` — `"Los años de experiencia no pueden ser negativos"` / `"Ingresa un correo válido"` / `"El correo ya se encuentra registrado"` / `"La especialidad X no existe"`
    /// - `401` — sin sesión.
    /// - `403` — no es psicólogo / no está activo / no está aprobado.
    /// </remarks>
    /// <response code="200">Perfil actualizado.</response>
    /// <response code="400">Validación HU.</response>
    /// <response code="401">Sesión requerida.</response>
    /// <response code="403">Solo psicólogos activos y aprobados.</response>
    [HttpPut]
    [ProducesResponseType(typeof(MessageResult), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ErrorResult), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ErrorResult), StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(typeof(ErrorResult), StatusCodes.Status403Forbidden)]
    public async Task<IActionResult> Actualizar([FromBody] ActualizarPerfilProfesionalVm vm, CancellationToken ct)
    {
        try
        {
            var ip = HttpContext.Connection.RemoteIpAddress?.ToString();
            var mensaje = await psicologos.ActualizarPerfilProfesionalAsync(ReadOptionalBearerToken(), vm, ip, ct);
            return Ok(new { mensaje });
        }
        catch (UnauthorizedAccessException ex)
        {
            return Unauthorized(new { error = ex.Message });
        }
        catch (InvalidOperationException ex) when (EsSinPermisos(ex.Message))
        {
            return StatusCode(StatusCodes.Status403Forbidden, new { error = ex.Message });
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(new { error = ex.Message });
        }
    }

    /// <summary>Sube una nueva tarjeta profesional (PDF, máx 5MB).</summary>
    /// <remarks>
    /// PB-25. Al cambiar o agregar especialidades hay que subir la nueva tarjeta.
    /// Queda pendiente de verificación del admin; el psicólogo NO pierde acceso,
    /// solo se ocultan las especialidades nuevas hasta la verificación.
    ///
    /// Form-data: `tarjeta` (PDF).
    ///
    /// Respuestas:
    /// - `200` — `{ "mensaje": "Tarjeta recibida. ..." }`
    /// - `400` — tarjeta inválida (no PDF / supera 5MB / vacía).
    /// - `401` — sin sesión.
    /// - `403` — solo psicólogos activos y aprobados.
    /// </remarks>
    /// <response code="200">Tarjeta recibida.</response>
    /// <response code="400">Archivo inválido.</response>
    /// <response code="401">Sesión requerida.</response>
    /// <response code="403">Solo psicólogos activos y aprobados.</response>
    [HttpPost("tarjeta")]
    [ProducesResponseType(typeof(MessageResult), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ErrorResult), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ErrorResult), StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(typeof(ErrorResult), StatusCodes.Status403Forbidden)]
    public async Task<IActionResult> SubirTarjeta([FromForm] IFormFile? tarjeta, CancellationToken ct)
    {
        try
        {
            var ip = HttpContext.Connection.RemoteIpAddress?.ToString();
            var mensaje = await psicologos.SubirTarjetaAsync(ReadOptionalBearerToken(), tarjeta, ip, ct);
            return Ok(new { mensaje });
        }
        catch (UnauthorizedAccessException ex)
        {
            return Unauthorized(new { error = ex.Message });
        }
        catch (InvalidOperationException ex) when (EsSinPermisos(ex.Message))
        {
            return StatusCode(StatusCodes.Status403Forbidden, new { error = ex.Message });
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(new { error = ex.Message });
        }
    }

    /// <summary>True para mensajes de permisos que deben responder 403.</summary>
    private static bool EsSinPermisos(string mensaje) => mensaje is "No tienes permisos para acceder a esta función"
        or "La cuenta no está activa." or "El psicólogo aún no está aprobado.";

    /// <summary>Lee opcionalmente el token de sesión.</summary>
    private string? ReadOptionalBearerToken()
    {
        if (!Request.Headers.TryGetValue("Authorization", out var v)) return null;
        var parts = v.ToString().Trim().Split(' ', StringSplitOptions.RemoveEmptyEntries);
        var token = parts.Length == 1 ? parts[0] : parts[^1];
        return token.Trim().Trim('"').Trim('\'');
    }
}