using Microsoft.AspNetCore.Mvc;
using Mindzyxs_API.Dtos;
using Mindzyxs_API.Services;

namespace Mindzyxs_API.Controllers;

/// <summary>
/// Administración de líneas de ayuda (PB-29 HU1-PB29):
/// lista con búsqueda, crear, editar, activar, inactivar y eliminar (borrado lógico).
/// Solo administrador autenticado.
/// </summary>
[ApiController]
[Route("api/admin/lineas-ayuda")]
public class AdminLineasAyudaController(AdminLineasAyudaService lineas) : ControllerBase
{
    /// <summary>Lista todas las líneas de ayuda (cualquier estado) con búsqueda opcional.</summary>
    /// <remarks>
    /// PB-29 HU1-PB29. Requiere administrador.
    /// `q` filtra por nombre; ordena por id.
    ///
    /// Respuestas:
    /// - `200` — `[ { "id", "nombre", "telefono", "url", "estado" } ]`
    /// - `401` — sin sesión.
    /// - `403` — no es administrador.
    /// </remarks>
    /// <response code="200">Lista de líneas de ayuda.</response>
    /// <response code="401">Sesión requerida.</response>
    /// <response code="403">Solo administradores.</response>
    [HttpGet]
    [ProducesResponseType(typeof(List<LineaAyudaAdminListaDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ErrorResult), StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(typeof(ErrorResult), StatusCodes.Status403Forbidden)]
    public async Task<IActionResult> Listar([FromQuery] string? q, CancellationToken ct)
    {
        try
        {
            return Ok(await lineas.ListarAsync(ReadBearerToken(), q, ct));
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

    /// <summary>Detalle de una línea de ayuda para editar.</summary>
    /// <remarks>
    /// PB-29 HU1-PB29. Requiere administrador. Prellena la pantalla Editar.
    ///
    /// Respuestas:
    /// - `200` — detalle completo de la línea
    /// - `401` — sin sesión.
    /// - `403` — no es administrador.
    /// - `404` — no existe.
    /// </remarks>
    /// <response code="200">Detalle de la línea de ayuda.</response>
    /// <response code="401">Sesión requerida.</response>
    /// <response code="403">Solo administradores.</response>
    /// <response code="404">No existe.</response>
    [HttpGet("{id:int}")]
    [ProducesResponseType(typeof(LineaAyudaAdminDetalleDto), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ErrorResult), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(ErrorResult), StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(typeof(ErrorResult), StatusCodes.Status403Forbidden)]
    public async Task<IActionResult> Detalle(int id, CancellationToken ct)
    {
        try
        {
            return Ok(await lineas.ObtenerDetalleAsync(ReadBearerToken(), id, ct));
        }
        catch (UnauthorizedAccessException ex)
        {
            return Unauthorized(new { error = ex.Message });
        }
        catch (InvalidOperationException ex) when (ex.Message == "Línea de ayuda no encontrada.")
        {
            return NotFound(new { error = ex.Message });
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(new { error = ex.Message });
        }
    }

    /// <summary>Crea una nueva línea de ayuda (estado Activa por defecto).</summary>
    /// <remarks>
    /// PB-29 HU1-PB29. Requiere administrador.
    /// Valida: nombre obligatorio + al menos un contacto (teléfono o URL).
    /// Estado inicial: Activa (visible a usuarios). Guarda `LINEA_AYUDA_CREADA` en `dbo.auth`.
    ///
    /// Respuestas:
    /// - `200` — `{ "id": 15, "mensaje": "Línea de ayuda creada correctamente" }`
    /// - `400` — `"Debes ingresar nombre y al menos un medio de contacto (teléfono o URL)"`
    /// - `401` — sin sesión.
    /// - `403` — no es administrador.
    /// </remarks>
    /// <response code="200">Línea creada.</response>
    /// <response code="400">Validación HU.</response>
    /// <response code="401">Sesión requerida.</response>
    /// <response code="403">Solo administradores.</response>
    [HttpPost]
    [ProducesResponseType(typeof(LineaAyudaAdminResultadoDto), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ErrorResult), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ErrorResult), StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(typeof(ErrorResult), StatusCodes.Status403Forbidden)]
    public async Task<IActionResult> Crear([FromBody] LineaAyudaAdminCrearDto dto, CancellationToken ct)
    {
        try
        {
            var ip = HttpContext.Connection.RemoteIpAddress?.ToString();
            return Ok(await lineas.CrearAsync(ReadBearerToken(), dto, ip, ct));
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

    /// <summary>Edita una línea de ayuda existente.</summary>
    /// <remarks>
    /// PB-29 HU1-PB29. Requiere administrador.
    /// Valida: nombre obligatorio + al menos un contacto (teléfono o URL).
    /// Guarda `LINEA_AYUDA_EDITADA` en `dbo.auth`.
    ///
    /// Respuestas:
    /// - `200` — `{ "id": ..., "mensaje": "Línea de ayuda actualizada correctamente" }`
    /// - `400` — `"Debes ingresar nombre y al menos un medio de contacto (teléfono o URL)"`
    /// - `401` — sin sesión.
    /// - `403` — no es administrador.
    /// - `404` — no existe.
    /// </remarks>
    /// <response code="200">Línea actualizada.</response>
    /// <response code="400">Validación HU.</response>
    /// <response code="401">Sesión requerida.</response>
    /// <response code="403">Solo administradores.</response>
    /// <response code="404">No existe.</response>
    [HttpPut("{id:int}")]
    [ProducesResponseType(typeof(LineaAyudaAdminResultadoDto), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ErrorResult), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ErrorResult), StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(typeof(ErrorResult), StatusCodes.Status403Forbidden)]
    [ProducesResponseType(typeof(ErrorResult), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Actualizar(int id, [FromBody] LineaAyudaAdminCrearDto dto, CancellationToken ct)
    {
        try
        {
            var ip = HttpContext.Connection.RemoteIpAddress?.ToString();
            return Ok(await lineas.ActualizarAsync(ReadBearerToken(), id, dto, ip, ct));
        }
        catch (UnauthorizedAccessException ex)
        {
            return Unauthorized(new { error = ex.Message });
        }
        catch (InvalidOperationException ex) when (ex.Message == "Línea de ayuda no encontrada.")
        {
            return NotFound(new { error = ex.Message });
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(new { error = ex.Message });
        }
    }

    /// <summary>Activa una línea de ayuda (estado = Activa, visible a usuarios).</summary>
    /// <remarks>
    /// PB-29 HU1-PB29. Requiere administrador.
    /// Guarda `LINEA_AYUDA_ACTIVADA` en `dbo.auth`.
    ///
    /// Respuestas:
    /// - `200` — `{ "mensaje": "Línea de ayuda activada correctamente" }`
    /// - `401` — sin sesión.
    /// - `403` — no es administrador.
    /// - `404` — no existe.
    /// </remarks>
    /// <response code="200">Activada correctamente.</response>
    /// <response code="401">Sesión requerida.</response>
    /// <response code="403">Solo administradores.</response>
    /// <response code="404">No existe.</response>
    [HttpPost("{id:int}/activar")]
    [ProducesResponseType(typeof(MessageResult), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ErrorResult), StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(typeof(ErrorResult), StatusCodes.Status403Forbidden)]
    [ProducesResponseType(typeof(ErrorResult), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Activar(int id, CancellationToken ct)
    {
        try
        {
            var ip = HttpContext.Connection.RemoteIpAddress?.ToString();
            var mensaje = await lineas.ActivarAsync(ReadBearerToken(), id, ip, ct);
            return Ok(new { mensaje });
        }
        catch (UnauthorizedAccessException ex)
        {
            return Unauthorized(new { error = ex.Message });
        }
        catch (InvalidOperationException ex) when (ex.Message == "Línea de ayuda no encontrada.")
        {
            return NotFound(new { error = ex.Message });
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(new { error = ex.Message });
        }
    }

    /// <summary>Inactiva una línea de ayuda (estado = Inactiva, oculta a usuarios).</summary>
    /// <remarks>
    /// PB-29 HU1-PB29. Requiere administrador.
    /// Guarda `LINEA_AYUDA_INACTIVADA` en `dbo.auth`.
    ///
    /// Respuestas:
    /// - `200` — `{ "mensaje": "Línea de ayuda inactivada correctamente" }`
    /// - `401` — sin sesión.
    /// - `403` — no es administrador.
    /// - `404` — no existe.
    /// </remarks>
    /// <response code="200">Inactivada correctamente.</response>
    /// <response code="401">Sesión requerida.</response>
    /// <response code="403">Solo administradores.</response>
    /// <response code="404">No existe.</response>
    [HttpPost("{id:int}/inactivar")]
    [ProducesResponseType(typeof(MessageResult), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ErrorResult), StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(typeof(ErrorResult), StatusCodes.Status403Forbidden)]
    [ProducesResponseType(typeof(ErrorResult), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Inactivar(int id, CancellationToken ct)
    {
        try
        {
            var ip = HttpContext.Connection.RemoteIpAddress?.ToString();
            var mensaje = await lineas.InactivarAsync(ReadBearerToken(), id, ip, ct);
            return Ok(new { mensaje });
        }
        catch (UnauthorizedAccessException ex)
        {
            return Unauthorized(new { error = ex.Message });
        }
        catch (InvalidOperationException ex) when (ex.Message == "Línea de ayuda no encontrada.")
        {
            return NotFound(new { error = ex.Message });
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(new { error = ex.Message });
        }
    }

    /// <summary>Elimina una línea de ayuda (borrado lógico: estado = Eliminada).</summary>
    /// <remarks>
    /// PB-29 HU1-PB29. Requiere administrador.
    /// Borrado lógico: cambia estado a Eliminada (no rompe historial ni FK).
    /// Guarda `LINEA_AYUDA_ELIMINADA` en `dbo.auth`.
    ///
    /// Respuestas:
    /// - `200` — `{ "mensaje": "Línea de ayuda eliminada correctamente" }`
    /// - `401` — sin sesión.
    /// - `403` — no es administrador.
    /// - `404` — no existe.
    /// </remarks>
    /// <response code="200">Eliminada correctamente.</response>
    /// <response code="401">Sesión requerida.</response>
    /// <response code="403">Solo administradores.</response>
    /// <response code="404">No existe.</response>
    [HttpDelete("{id:int}")]
    [ProducesResponseType(typeof(MessageResult), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ErrorResult), StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(typeof(ErrorResult), StatusCodes.Status403Forbidden)]
    [ProducesResponseType(typeof(ErrorResult), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Eliminar(int id, CancellationToken ct)
    {
        try
        {
            var ip = HttpContext.Connection.RemoteIpAddress?.ToString();
            var mensaje = await lineas.EliminarAsync(ReadBearerToken(), id, ip, ct);
            return Ok(new { mensaje });
        }
        catch (UnauthorizedAccessException ex)
        {
            return Unauthorized(new { error = ex.Message });
        }
        catch (InvalidOperationException ex) when (ex.Message == "Línea de ayuda no encontrada.")
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