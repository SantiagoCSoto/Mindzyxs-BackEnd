using Microsoft.AspNetCore.Mvc;
using Mindzyxs_API.Dtos;
using Mindzyxs_API.Services;

namespace Mindzyxs_API.Controllers;

/// <summary>
/// Cuestionarios predeterminados del administrador (PB-28 HU1-PB28):
/// lista con búsqueda, crear, editar y desactivar.
/// Solo administrador autenticado.
/// </summary>
[ApiController]
[Route("api/admin/cuestionarios-predeterminados")]
public class AdminCuestionariosController(AdminCuestionariosService cuestionarios) : ControllerBase
{
    /// <summary>Lista los cuestionarios predeterminados con búsqueda opcional.</summary>
    /// <remarks>
    /// PB-28 HU1-PB28. Requiere administrador.
    /// `q` filtra por título; ordena por id.
    ///
    /// Respuestas:
    /// - `200` — `[ { "id", "titulo", "descripcion", "estado", "numPreguntas", "fechaCreacion" } ]`
    /// - `401` — sin sesión.
    /// - `403` — no es administrador.
    /// </remarks>
    /// <response code="200">Lista de cuestionarios predeterminados.</response>
    /// <response code="401">Sesión requerida.</response>
    /// <response code="403">Solo administradores.</response>
    [HttpGet]
    [ProducesResponseType(typeof(List<CuestionarioPredeterminadoListaDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ErrorResult), StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(typeof(ErrorResult), StatusCodes.Status403Forbidden)]
    public async Task<IActionResult> Listar([FromQuery] string? q, CancellationToken ct)
    {
        try
        {
            return Ok(await cuestionarios.ListarAsync(ReadBearerToken(), q, ct));
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

    /// <summary>Detalle editable de un cuestionario predeterminado.</summary>
    /// <remarks>
    /// PB-28 HU1-PB28. Requiere administrador. Prellena la pantalla
    /// Editar (título, descripción, estado, umbrales y preguntas con sus ids).
    ///
    /// Respuestas:
    /// - `200` — `{ "id", "titulo", "descripcion", "estado", "modoCalculo", "umbralBajoMax", "umbralMedioMax", "fechaCreacion", "preguntas": [...] }`
    /// - `401` — sin sesión.
    /// - `403` — no es administrador.
    /// - `404` — no existe.
    /// </remarks>
    /// <response code="200">Detalle del cuestionario.</response>
    /// <response code="401">Sesión requerida.</response>
    /// <response code="403">Solo administradores.</response>
    /// <response code="404">No existe.</response>
    [HttpGet("{id:int}")]
    [ProducesResponseType(typeof(CuestionarioPredeterminadoDetalleDto), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ErrorResult), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(ErrorResult), StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(typeof(ErrorResult), StatusCodes.Status403Forbidden)]
    public async Task<IActionResult> Detalle(int id, CancellationToken ct)
    {
        try
        {
            return Ok(await cuestionarios.ObtenerDetalleAsync(ReadBearerToken(), id, ct));
        }
        catch (UnauthorizedAccessException ex)
        {
            return Unauthorized(new { error = ex.Message });
        }
        catch (InvalidOperationException ex) when (ex.Message == "Cuestionario predeterminado no encontrado.")
        {
            return NotFound(new { error = ex.Message });
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(new { error = ex.Message });
        }
    }

    /// <summary>Crea un cuestionario predeterminado (Borrador/Inactivo o Publicado).</summary>
    /// <remarks>
    /// PB-28 HU1-PB28. Requiere administrador. Igual que crear cuestionario de psicólogo
    /// pero `Tipo = "Predeterminado"`. `Publicar: true` -> estado Publicado; `false` -> Inactivo.
    /// Valida título obligatorio y al menos una pregunta.
    /// Guarda `CUESTIONARIO_PREDETERMINADO_CREADO` en `dbo.auth`.
    ///
    /// Respuestas:
    /// - `200` — `{ "id": 10, "mensaje": "Cuestionario predeterminado guardado correctamente" }`
    /// - `400` — `"Debes completar todos los campos obligatorios"` (título vacío o sin preguntas)
    /// - `401` — sin sesión.
    /// - `403` — no es administrador.
    /// </remarks>
    /// <response code="200">Cuestionario creado.</response>
    /// <response code="400">Validación HU.</response>
    /// <response code="401">Sesión requerida.</response>
    /// <response code="403">Solo administradores.</response>
    [HttpPost]
    [ProducesResponseType(typeof(CuestionarioPredeterminadoResultadoDto), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ErrorResult), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ErrorResult), StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(typeof(ErrorResult), StatusCodes.Status403Forbidden)]
    public async Task<IActionResult> Crear([FromBody] CuestionarioPredeterminadoCrearDto dto, CancellationToken ct)
    {
        try
        {
            var ip = HttpContext.Connection.RemoteIpAddress?.ToString();
            return Ok(await cuestionarios.CrearAsync(ReadBearerToken(), dto, ip, ct));
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

    /// <summary>Edita un cuestionario predeterminado existente.</summary>
    /// <remarks>
    /// PB-28 HU1-PB28. Requiere administrador. Igual cuerpo que crear + `id` opcional
    /// por pregunta (nulo = nueva; `id` de una pregunta que ya no venga se da de baja
    /// con `eliminada`). Valida título y al menos una pregunta.
    /// Guarda `CUESTIONARIO_PREDETERMINADO_EDITADO` en `dbo.auth`.
    ///
    /// Respuestas:
    /// - `200` — `{ "id": ..., "mensaje": "Cuestionario predeterminado guardado correctamente" }`
    /// - `400` — `"Debes completar todos los campos obligatorios"` (título vacío o sin preguntas)
    /// - `401` — sin sesión.
    /// - `403` — no es administrador.
    /// - `404` — no existe.
    /// </remarks>
    /// <response code="200">Cuestionario actualizado.</response>
    /// <response code="400">Validación HU.</response>
    /// <response code="401">Sesión requerida.</response>
    /// <response code="403">Solo administradores.</response>
    /// <response code="404">No existe.</response>
    [HttpPut("{id:int}")]
    [ProducesResponseType(typeof(CuestionarioPredeterminadoResultadoDto), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ErrorResult), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ErrorResult), StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(typeof(ErrorResult), StatusCodes.Status403Forbidden)]
    [ProducesResponseType(typeof(ErrorResult), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Actualizar(int id, [FromBody] CuestionarioPredeterminadoCrearDto dto, CancellationToken ct)
    {
        try
        {
            var ip = HttpContext.Connection.RemoteIpAddress?.ToString();
            return Ok(await cuestionarios.ActualizarAsync(ReadBearerToken(), id, dto, ip, ct));
        }
        catch (UnauthorizedAccessException ex)
        {
            return Unauthorized(new { error = ex.Message });
        }
        catch (InvalidOperationException ex) when (ex.Message == "Cuestionario predeterminado no encontrado.")
        {
            return NotFound(new { error = ex.Message });
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(new { error = ex.Message });
        }
    }

    /// <summary>Desactiva un cuestionario predeterminado (estado = Inactivo).</summary>
    /// <remarks>
    /// PB-28 HU1-PB28. Requiere administrador. El cuestionario pasa a estado Inactivo
    /// y deja de aparecer en la asignación a pacientes.
    /// Guarda `CUESTIONARIO_PREDETERMINADO_DESACTIVADO` en `dbo.auth`.
    ///
    /// Respuestas:
    /// - `200` — `{ "mensaje": "Cuestionario predeterminado desactivado correctamente" }`
    /// - `401` — sin sesión.
    /// - `403` — no es administrador.
    /// - `404` — no existe.
    /// </remarks>
    /// <response code="200">Desactivado correctamente.</response>
    /// <response code="401">Sesión requerida.</response>
    /// <response code="403">Solo administradores.</response>
    /// <response code="404">No existe.</response>
    [HttpPost("{id:int}/desactivar")]
    [ProducesResponseType(typeof(MessageResult), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ErrorResult), StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(typeof(ErrorResult), StatusCodes.Status403Forbidden)]
    [ProducesResponseType(typeof(ErrorResult), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Desactivar(int id, CancellationToken ct)
    {
        try
        {
            var ip = HttpContext.Connection.RemoteIpAddress?.ToString();
            var mensaje = await cuestionarios.DesactivarAsync(ReadBearerToken(), id, ip, ct);
            return Ok(new { mensaje });
        }
        catch (UnauthorizedAccessException ex)
        {
            return Unauthorized(new { error = ex.Message });
        }
        catch (InvalidOperationException ex) when (ex.Message == "Cuestionario predeterminado no encontrado.")
        {
            return NotFound(new { error = ex.Message });
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(new { error = ex.Message });
        }
    }

    /// <summary>Lee el token Bearer del header Authorization.</summary>
    private string? ReadBearerToken()
    {
        if (!Request.Headers.TryGetValue("Authorization", out var v)) return null;
        var parts = v.ToString().Trim().Split(' ', StringSplitOptions.RemoveEmptyEntries);
        var token = parts.Length == 1 ? parts[0] : parts[^1];
        return token.Trim().Trim('"').Trim('\'');
    }
}