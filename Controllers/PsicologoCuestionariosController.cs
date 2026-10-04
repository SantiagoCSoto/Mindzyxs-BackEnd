using Microsoft.AspNetCore.Mvc;
using Mindzyxs_API.Dtos;
using Mindzyxs_API.Services;

namespace Mindzyxs_API.Controllers;

/// <summary>
/// Cuestionarios del psicólogo (PB-20 HU1-PB20): crea cuestionarios personalizados
/// (Borrador o Publicados) con preguntas, opciones con valor e intervalos de
/// riesgo configurables. Solo psicólogo activo y aprobado.
///
/// Gestión (PB-21 HU1-PB21): lista con búsqueda, detalle, edición (un Publicado
/// pasa a Borrador con confirmación antes de habilitar la edición), guardado como
/// borrador/publicación con validación de mínimo una pregunta, y eliminación con
/// confirmación extra (baja lógica, fuera de los análisis + observaciones borradas).
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

    /// <summary>Lista "Mis cuestionarios" del psicólogo (Borrador/Publicado) con búsqueda.</summary>
    /// <remarks>
    /// PB-21 HU1-PB21. Requiere psicólogo. `q` filtra por título; ordena recientes primero.
    /// Los Eliminados no aparecen.
    ///
    /// Respuestas:
    /// - `200` — `[ { "id", "titulo", "descripcion", "estado", "numPreguntas", "fechaCreacion" } ]`
    /// - `401` — sin sesión.
    /// - `403` — sin rol/autorización.
    /// </remarks>
    /// <response code="200">Lista de mis cuestionarios.</response>
    /// <response code="401">Sesión requerida.</response>
    /// <response code="403">Solo psicólogos.</response>
    [HttpGet]
    [ProducesResponseType(typeof(List<CuestionarioPsicologoListaDto>), StatusCodes.Status200OK)]
    public async Task<IActionResult> Listar([FromQuery] string? q, CancellationToken ct)
    {
        try
        {
            return Ok(await cuestionarios.ListarAsync(ReadOptionalBearerToken(), q, ct));
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

    /// <summary>Detalle editable de un cuestionario propio (preguntas y opciones con ids).</summary>
    /// <remarks>
    /// PB-21 HU1-PB21. Requiere psicólogo dueño del cuestionario. Prellena la pantalla
    /// Editar (título, descripción, estado, umbrales y preguntas con sus ids para mapear
    /// el guardado).
    ///
    /// Respuestas:
    /// - `200` — `{ "id", "titulo", "descripcion", "estado", "modoCalculo", "umbralBajoMax", "umbralMedioMax", "fechaCreacion", "preguntas": [...] }`
    /// - `401` — sin sesión.
    /// - `403` — sin rol/autorización.
    /// - `404` — no existe o no es mío.
    /// </remarks>
    /// <response code="200">Detalle del cuestionario.</response>
    /// <response code="401">Sesión requerida.</response>
    /// <response code="403">Solo psicólogos.</response>
    /// <response code="404">No existe o no es mío.</response>
    [HttpGet("{id:int}")]
    [ProducesResponseType(typeof(CuestionarioPsicologoDetalleDto), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ErrorResult), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Detalle(int id, CancellationToken ct)
    {
        try
        {
            return Ok(await cuestionarios.ObtenerDetalleAsync(ReadOptionalBearerToken(), id, ct));
        }
        catch (UnauthorizedAccessException ex)
        {
            return Unauthorized(new { error = ex.Message });
        }
        catch (InvalidOperationException ex) when (EsSinPermisos(ex.Message))
        {
            return StatusCode(StatusCodes.Status403Forbidden, new { error = ex.Message });
        }
        catch (InvalidOperationException ex) when (ex.Message == "Cuestionario no encontrado.")
        {
            return NotFound(new { error = ex.Message });
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(new { error = ex.Message });
        }
    }

    /// <summary>Pasa un cuestionario Publicado a Borrador (confirmación del psicólogo antes de editar).</summary>
    /// <remarks>
    /// PB-21 HU1-PB21. Es el paso previo de la edición: si está publicado, el front
    /// pide confirmación y este endpoint lo deja en Borrador (con log) para habilitar
    /// el editor. Guarda `CUESTIONARIO_EDITADO` en `dbo.auth`.
    ///
    /// Respuestas:
    /// - `200` — `{ "mensaje": "El cuestionario ahora es un borrador" }`
    /// - `400` — `{ "error": "El cuestionario no está publicado." }`
    /// - `401` — sin sesión.
    /// - `403` — sin rol/autorización.
    /// - `404` — no existe o no es mío.
    /// </remarks>
    /// <response code="200">Pasado a Borrador.</response>
    /// <response code="400">No está publicado.</response>
    /// <response code="401">Sesión requerida.</response>
    /// <response code="403">Solo psicólogos.</response>
    /// <response code="404">No existe o no es mío.</response>
    [HttpPost("{id:int}/despublicar")]
    [ProducesResponseType(typeof(MessageResult), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ErrorResult), StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> Despublicar(int id, CancellationToken ct)
    {
        try
        {
            var ip = HttpContext.Connection.RemoteIpAddress?.ToString();
            var mensaje = await cuestionarios.DespublicarAsync(ReadOptionalBearerToken(), id, ip, ct);
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
        catch (InvalidOperationException ex) when (ex.Message == "Cuestionario no encontrado.")
        {
            return NotFound(new { error = ex.Message });
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(new { error = ex.Message });
        }
    }

    /// <summary>Guarda la edición del cuestionario (Borrador o Publicado) con sus preguntas y opciones.</summary>
    /// <remarks>
    /// PB-21 HU1-PB21. Requiere psicólogo dueño. Mismo cuerpo que crear + `id` opcional
    /// por pregunta (preguntas nuevas sin id). Valida al menos una pregunta
    /// ("El cuestionario debe tener al menos una pregunta"). Un Publicado primero debe
    /// pasar a Borrador (`POST .../{id}/despublicar`). Guarda `CUESTIONARIO_EDITADO` en `dbo.auth`.
    ///
    /// Respuestas:
    /// - `200` — `{ "id": ..., "mensaje": "Cuestionario actualizado correctamente" }`
    /// - `400` — `"El cuestionario debe tener al menos una pregunta"` u otra validación HU
    /// - `400` — `"Para modificar un cuestionario publicado pásalo primero a borrador."`
    /// - `401` — sin sesión.
    /// - `403` — sin rol/autorización.
    /// - `404` — no existe o no es mío.
    /// </remarks>
    /// <response code="200">Cuestionario actualizado.</response>
    /// <response code="400">Validación HU.</response>
    /// <response code="401">Sesión requerida.</response>
    /// <response code="403">Solo psicólogos.</response>
    /// <response code="404">No existe o no es mío.</response>
    [HttpPut("{id:int}")]
    [ProducesResponseType(typeof(CuestionarioCreadoDto), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ErrorResult), StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> Actualizar(int id, [FromBody] CuestionarioCrearDto dto, CancellationToken ct)
    {
        try
        {
            var ip = HttpContext.Connection.RemoteIpAddress?.ToString();
            return Ok(await cuestionarios.ActualizarAsync(ReadOptionalBearerToken(), id, dto, ip, ct));
        }
        catch (UnauthorizedAccessException ex)
        {
            return Unauthorized(new { error = ex.Message });
        }
        catch (InvalidOperationException ex) when (EsSinPermisos(ex.Message))
        {
            return StatusCode(StatusCodes.Status403Forbidden, new { error = ex.Message });
        }
        catch (InvalidOperationException ex) when (ex.Message == "Cuestionario no encontrado.")
        {
            return NotFound(new { error = ex.Message });
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(new { error = ex.Message });
        }
    }

    /// <summary>Elimina un cuestionario propio con confirmación extra (baja lógica + observaciones).</summary>
    /// <remarks>
    /// PB-21 HU1-PB21. Requiere `?confirmar=true` (la confirmación extra del HU). Pasa a
    /// Estado "Eliminado" (deja de contarse en los análisis; los eventos ya respondidos
    /// se conservan) y borra las observaciones asociadas a los eventos de ese cuestionario.
    /// Guarda `CUESTIONARIO_ELIMINADO` en `dbo.auth`.
    ///
    /// Respuestas:
    /// - `200` — `{ "mensaje": "Cuestionario eliminado correctamente" }`
    /// - `400` — `{ "error": "Debes confirmar la eliminación del cuestionario." }` (sin confirmar)
    /// - `401` — sin sesión.
    /// - `403` — sin rol/autorización.
    /// - `404` — no existe o no es mío.
    /// </remarks>
    /// <response code="200">Cuestionario eliminado.</response>
    /// <response code="400">Falta la confirmación.</response>
    /// <response code="401">Sesión requerida.</response>
    /// <response code="403">Solo psicólogos.</response>
    /// <response code="404">No existe o no es mío.</response>
    [HttpDelete("{id:int}")]
    [ProducesResponseType(typeof(MessageResult), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ErrorResult), StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> Eliminar(int id, [FromQuery] bool? confirmar, CancellationToken ct)
    {
        try
        {
            var ip = HttpContext.Connection.RemoteIpAddress?.ToString();
            var mensaje = await cuestionarios.EliminarAsync(ReadOptionalBearerToken(), id, confirmar ?? false, ip, ct);
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
        catch (InvalidOperationException ex) when (ex.Message == "Cuestionario no encontrado.")
        {
            return NotFound(new { error = ex.Message });
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(new { error = ex.Message });
        }
    }

    /// <summary>True para mensajes de permisos que deben responder 403 (PB-20/21).</summary>
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
