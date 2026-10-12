using Microsoft.AspNetCore.Mvc;
using Mindzyxs_API.Dtos;
using Mindzyxs_API.Services;

namespace Mindzyxs_API.Controllers;

/// <summary>
/// Gestión de cuentas de usuarios (PB-25 HU1-PB25, fiel a v1 CU25):
/// lista con filtros, detalle, edición, cambio de estado y restablecimiento
/// forzado. Solo administradores. Nunca expone ni devuelve passwords.
/// </summary>
[ApiController]
[Route("api/admin/usuarios")]
public class AdminUsuariosController(AdminService admin) : ControllerBase
{
    /// <summary>Lista usuarios (sin admins) con filtros rol/estado/búsqueda.</summary>
    /// <remarks>
    /// Filtros opcionales: `q` (nombre/apellido/correo), `rol` (Paciente/Psicologo),
    /// `estado` (Activa/PendienteVerificacion/...). Vacía → el front muestra
    /// `No se encontraron usuarios con los criterios seleccionados.`
    ///
    /// Respuestas:
    /// - `200` — `{ "usuarios": [ { "id", "nombre", "correo", "rol", "estado", "aprobacion", "fechaRegistro" } ] }`
    /// - `403` — sin rol Administrador.
    /// </remarks>
    /// <response code="200">Usuarios.</response>
    /// <response code="403">Solo administradores.</response>
    [HttpGet]
    [ProducesResponseType(typeof(List<UsuarioAdminDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ErrorResult), StatusCodes.Status403Forbidden)]
    public async Task<IActionResult> Listar([FromQuery] string? q, [FromQuery] string? rol, [FromQuery] string? estado, CancellationToken ct)
    {
        var (ok, _, reason) = await EnsureAdminAsync(ct);
        if (!ok) return StatusCode(403, new { error = reason });

        try
        {
            var r = await admin.UsuariosAsync(q, rol, estado, ct);
            if (!r.Success) return BadRequest(new { error = r.Message });
            return Ok(new { usuarios = r.Data });
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(new { error = "Filtro no válido." });
        }
    }

    /// <summary>Detalle del usuario (sin password).</summary>
    /// <remarks>
    /// Respuestas: `200` detalle; `403` sin rol o es admin; `404` no existe.
    /// </remarks>
    /// <response code="200">Detalle.</response>
    /// <response code="403">Solo administradores / no administrable.</response>
    /// <response code="404">No existe.</response>
    [HttpGet("{idUsuario:int}")]
    [ProducesResponseType(typeof(UsuarioDetalleDto), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ErrorResult), StatusCodes.Status403Forbidden)]
    [ProducesResponseType(typeof(ErrorResult), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Detalle(int idUsuario, CancellationToken ct)
    {
        var (ok, _, reason) = await EnsureAdminAsync(ct);
        if (!ok) return StatusCode(403, new { error = reason });

        var r = await admin.DetalleUsuarioAsync(idUsuario, ct);
        if (!r.Success)
            return r.Message == "Usuario no encontrado."
                ? NotFound(new { error = r.Message })
                : StatusCode(StatusCodes.Status403Forbidden, new { error = r.Message });
        return Ok(r.Data);
    }

    /// <summary>Cambia el estado de la cuenta (Activa/Suspendida/Inactiva...).</summary>
    /// <remarks>
    /// Respuestas:
    /// - `200` — `{ "mensaje": "Estado actualizado correctamente." }`
    /// - `400` — estado vacío o inválido.
    /// - `403` — sin rol o es admin.
    /// - `404` — no existe.
    /// </remarks>
    /// <response code="200">Estado cambiado.</response>
    /// <response code="400">Validación.</response>
    /// <response code="403">Solo administradores / no administrable.</response>
    /// <response code="404">No existe.</response>
    [HttpPut("{idUsuario:int}/estado")]
    [ProducesResponseType(typeof(MessageResult), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ErrorResult), StatusCodes.Status403Forbidden)]
    [ProducesResponseType(typeof(ErrorResult), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> CambiarEstado(int idUsuario, [FromBody] CambiarEstadoVm vm, CancellationToken ct)
    {
        var (ok, adminId, reason) = await EnsureAdminAsync(ct);
        if (!ok) return StatusCode(403, new { error = reason });

        var ip = HttpContext.Connection.RemoteIpAddress?.ToString();
        var r = await admin.CambiarEstadoAsync(adminId!.Value, idUsuario, vm.NuevoEstado, ip, ct);
        if (!r.Success)
            return r.Message == "Usuario no encontrado."
                ? NotFound(new { error = r.Message })
                : r.Message == "No puedes administrar usuarios administradores."
                    ? StatusCode(StatusCodes.Status403Forbidden, new { error = r.Message })
                    : BadRequest(new { error = r.Message });
        return Ok(new { mensaje = r.Message });
    }

    /// <summary>Fuerza restablecimiento (envía código al correo del usuario).</summary>
    /// <remarks>
    /// Nunca devuelve ni expone passwords: solo dispara el flujo de recuperación.
    ///
    /// Respuestas:
    /// - `200` — `{ "mensaje": "Se envió un código de restablecimiento al correo del usuario." }`
    /// - `403` — sin rol o es admin.
    /// - `404` — no existe.
    /// </remarks>
    /// <response code="200">Código enviado.</response>
    /// <response code="403">Solo administradores / no administrable.</response>
    /// <response code="404">No existe.</response>
    [HttpPost("{idUsuario:int}/reset-password")]
    [ProducesResponseType(typeof(MessageResult), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ErrorResult), StatusCodes.Status403Forbidden)]
    [ProducesResponseType(typeof(ErrorResult), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> ForzarReset(int idUsuario, CancellationToken ct)
    {
        var (ok, adminId, reason) = await EnsureAdminAsync(ct);
        if (!ok) return StatusCode(403, new { error = reason });

        var ip = HttpContext.Connection.RemoteIpAddress?.ToString();
        var r = await admin.ForzarResetAsync(adminId!.Value, idUsuario, ip, ct);
        if (!r.Success)
            return r.Message == "Usuario no encontrado."
                ? NotFound(new { error = r.Message })
                : r.Message == "No puedes administrar usuarios administradores."
                    ? StatusCode(StatusCodes.Status403Forbidden, new { error = r.Message })
                    : BadRequest(new { error = r.Message });
        return Ok(new { mensaje = r.Message });
    }

    /// <summary>Valida sesión activa de administrador.</summary>
    private async Task<(bool Ok, int? AdminId, string Reason)> EnsureAdminAsync(CancellationToken ct)
    {
        if (!Request.Headers.TryGetValue("Authorization", out var v)) return (false, null, "Tu sesión ha expirado. Inicia sesión nuevamente");
        var parts = v.ToString().Trim().Split(' ', StringSplitOptions.RemoveEmptyEntries);
        var token = parts.Length == 1 ? parts[0] : parts[^1];
        var ip = HttpContext.Connection.RemoteIpAddress?.ToString();
        // Reusa la validación central del AdminService.
        var adminId = await admin.GetAdminIdAsync(token.Trim().Trim('"').Trim('\''), ip, ct);
        if (adminId is null)
            return string.IsNullOrWhiteSpace(token)
                ? (false, null, "Tu sesión ha expirado. Inicia sesión nuevamente")
                : (false, null, "No tienes permisos para acceder a esta función");
        return (true, adminId, string.Empty);
    }
}
