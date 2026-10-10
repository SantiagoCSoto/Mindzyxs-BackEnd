using Microsoft.AspNetCore.Mvc;
using Mindzyxs_API.Dtos;
using Mindzyxs_API.Services;

namespace Mindzyxs_API.Controllers;

/// <summary>
/// Notificaciones del admin (PB-30, fiel a v1 CU31 + preview HU):
/// catálogo de eventos, plantillas con variables y canales.
/// Solo administradores.
/// </summary>
[ApiController]
[Route("api/admin/notificaciones")]
public class AdminNotificacionesController(AdminService admin) : ControllerBase
{
    /// <summary>Catálogo de eventos con canales y estado.</summary>
    /// <remarks>
    /// Respuestas:
    /// - `200` — `{ "notificaciones": [ { "tipo", "nombre", "canales": [], "activo", "asunto", "cuerpo" } ] }`
    /// - `403` — sin rol Administrador.
    /// </remarks>
    /// <response code="200">Catálogo.</response>
    /// <response code="403">Solo administradores.</response>
    [HttpGet]
    [ProducesResponseType(typeof(List<NotificacionDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ErrorResult), StatusCodes.Status403Forbidden)]
    public async Task<IActionResult> Listar(CancellationToken ct)
    {
        var (ok, _, reason) = await EnsureAdminAsync(ct);
        if (!ok) return StatusCode(403, new { error = reason });

        var r = await admin.NotificacionesAsync(ct);
        return Ok(new { notificaciones = r.Data });
    }

    /// <summary>Detalle de un evento del catálogo.</summary>
    /// <remarks>
    /// Respuestas: `200` detalle; `403` sin rol; `404` evento inexistente.
    /// </remarks>
    /// <response code="200">Detalle.</response>
    /// <response code="403">Solo administradores.</response>
    /// <response code="404">Evento no encontrado.</response>
    [HttpGet("{tipo}")]
    [ProducesResponseType(typeof(NotificacionDto), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ErrorResult), StatusCodes.Status403Forbidden)]
    [ProducesResponseType(typeof(ErrorResult), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Detalle(string tipo, CancellationToken ct)
    {
        var (ok, _, reason) = await EnsureAdminAsync(ct);
        if (!ok) return StatusCode(403, new { error = reason });

        var r = await admin.DetalleNotificacionAsync(tipo, ct);
        if (!r.Success) return NotFound(new { error = r.Message });
        return Ok(r.Data);
    }

    /// <summary>Actualiza canales, estado y plantilla del evento.</summary>
    /// <remarks>
    /// Respuestas:
    /// - `200` — `{ "mensaje": "Notificación actualizada correctamente." }`
    /// - `400` — `Debes habilitar al menos un canal para este evento.` / plantilla/asunto.
    /// - `403` — sin rol Administrador.
    /// - `404` — evento inexistente.
    /// </remarks>
    /// <response code="200">Actualizada.</response>
    /// <response code="400">Validación.</response>
    /// <response code="403">Solo administradores.</response>
    /// <response code="404">Evento no encontrado.</response>
    [HttpPut("{tipo}")]
    [ProducesResponseType(typeof(MessageResult), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ErrorResult), StatusCodes.Status403Forbidden)]
    [ProducesResponseType(typeof(ErrorResult), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Actualizar(string tipo, [FromBody] ActualizarNotificacionVm vm, CancellationToken ct)
    {
        var (ok, adminId, reason) = await EnsureAdminAsync(ct);
        if (!ok) return StatusCode(403, new { error = reason });

        var ip = HttpContext.Connection.RemoteIpAddress?.ToString();
        var r = await admin.ActualizarNotificacionAsync(adminId!.Value, tipo, vm, ip, ct);
        if (!r.Success)
            return r.Message == "Evento no encontrado."
                ? NotFound(new { error = r.Message })
                : BadRequest(new { error = r.Message });
        return Ok(new { mensaje = r.Message });
    }

    /// <summary>Preview con variables de ejemplo (no envía nada).</summary>
    /// <remarks>
    /// Respuestas: `200` asunto/cuerpo renderizados; `403` sin rol; `404` evento inexistente.
    /// </remarks>
    /// <response code="200">Preview.</response>
    /// <response code="403">Solo administradores.</response>
    /// <response code="404">Evento no encontrado.</response>
    [HttpPost("{tipo}/preview")]
    [ProducesResponseType(typeof(NotificacionDto), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ErrorResult), StatusCodes.Status403Forbidden)]
    [ProducesResponseType(typeof(ErrorResult), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Preview(string tipo, [FromBody] PreviewNotificacionVm? vm, CancellationToken ct)
    {
        var (ok, _, reason) = await EnsureAdminAsync(ct);
        if (!ok) return StatusCode(403, new { error = reason });

        var r = await admin.PreviewNotificacionAsync(tipo, vm ?? new PreviewNotificacionVm(), ct);
        if (!r.Success) return NotFound(new { error = r.Message });
        return Ok(r.Data);
    }

    /// <summary>Valida sesión activa de administrador.</summary>
    private async Task<(bool Ok, int? AdminId, string Reason)> EnsureAdminAsync(CancellationToken ct)
    {
        if (!Request.Headers.TryGetValue("Authorization", out var v)) return (false, null, "Tu sesión ha expirado. Inicia sesión nuevamente");
        var parts = v.ToString().Trim().Split(' ', StringSplitOptions.RemoveEmptyEntries);
        var token = parts.Length == 1 ? parts[0] : parts[^1];
        var ip = HttpContext.Connection.RemoteIpAddress?.ToString();
        var adminId = await admin.GetAdminIdAsync(token.Trim().Trim('"').Trim('\''), ip, ct);
        if (adminId is null)
            return string.IsNullOrWhiteSpace(token)
                ? (false, null, "Tu sesión ha expirado. Inicia sesión nuevamente")
                : (false, null, "No tienes permisos para acceder a esta función");
        return (true, adminId, string.Empty);
    }
}
