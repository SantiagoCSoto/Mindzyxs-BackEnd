using Microsoft.AspNetCore.Mvc;
using Mindzyxs_API.Dtos;
using Mindzyxs_API.Services;

namespace Mindzyxs_API.Controllers;

/// <summary>
/// Cuestionarios del psicólogo (PB-20 HU1-PB20): crea cuestionarios personalizados
/// (Borrador o Publicados) con preguntas, opciones con valor e intervalos de
/// riesgo configurables. Solo psicólogo activo y aprobado.
/// </summary>
[ApiController]
[Route("api/psicologo/cuestionarios")]
public class PsicologoCuestionariosController(PsicologoCuestionariosService cuestionarios) : ControllerBase
{
    /// <summary>Crea un cuestionario personalizado (Borrador o Publicado).</summary>
    /// <remarks>
    /// Ejemplo:
    ///
    ///     POST /api/psicologo/cuestionarios
    ///     {
    ///       "titulo": "Ansiedad semanal",
    ///       "descripcion": "Seguimiento de síntomas",
    ///       "publicar": false,
    ///       "umbrales": { "bajoMax": 6, "medioMax": 12 },
    ///       "preguntas": [
    ///         { "texto": "¿Con qué frecuencia se sintió nervioso?",
    ///           "tipo": "opcion_multiple",
    ///           "opciones": [ { "texto": "Nunca", "valor": 0 }, { "texto": "A veces", "valor": 2 }, { "texto": "Siempre", "valor": 4 } ] }
    ///       ]
    ///     }
    ///
    /// Sin `umbrales` rige la regla actual (promedio 1-2 Bajo, 3 Medio, 4-5 Alto).
    ///
    /// Respuestas:
    /// - `200` — `{ "id": 7, "mensaje": "Cuestionario guardado correctamente" }`
    /// - `400` — `Debes agregar al menos una pregunta.` (u otra validación HU).
    /// - `401` — sin sesión.
    /// - `403` — sin rol/autorización.
    /// </remarks>
    /// <response code="200">Cuestionario guardado.</response>
    /// <response code="400">Validación HU.</response>
    /// <response code="401">Sesión requerida.</response>
    /// <response code="403">Solo psicólogos.</response>
    [HttpPost]
    [ProducesResponseType(typeof(CuestionarioCreadoDto), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ErrorResult), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ErrorResult), StatusCodes.Status403Forbidden)]
    public async Task<IActionResult> Crear([FromBody] CuestionarioCrearDto dto, CancellationToken ct)
    {
        try
        {
            var ip = HttpContext.Connection.RemoteIpAddress?.ToString();
            return Ok(await cuestionarios.CrearAsync(ReadOptionalBearerToken(), dto, ip, ct));
        }
        catch (UnauthorizedAccessException ex)
        {
            return Unauthorized(new { error = ex.Message });
        }
        catch (InvalidOperationException ex) when (ex.Message == "No tienes permisos para acceder a esta función"
            || ex.Message == "La cuenta no está activa." || ex.Message == "El psicólogo aún no está aprobado.")
        {
            return StatusCode(StatusCodes.Status403Forbidden, new { error = ex.Message });
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(new { error = ex.Message });
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
