using Microsoft.AspNetCore.Mvc;
using Mindzyxs_API.Services;

namespace Mindzyxs_API.Controllers;

// PB-27 HU1-PB27: Administrar BD. Solo rol Administrador (token de sesión válido).
// Respaldos locales, restaurar, ver respaldos y mantenimiento. Todo logueado en dbo.auth.
[ApiController]
[Route("api/[controller]")]
public class AdminController(AdminService admin) : ControllerBase
{
    // Crear respaldo local (JSON snapshot). Registra en dbo.respaldos + dbo.auth.
    [HttpPost("backup")]
    public async Task<IActionResult> Backup(CancellationToken ct)
    {
        var (ok, adminId, reason) = await EnsureAdminAsync(ct);
        if (!ok) return StatusCode(403, new { error = reason });

        var r = await admin.CreateBackupAsync(adminId!.Value, HttpContext.Connection.RemoteIpAddress?.ToString(), ct);
        if (!r.Success) return StatusCode(500, new { error = r.Message });
        return StatusCode(201, new { mensaje = r.Message, respaldo = r.Data });
    }

    // Listar respaldos existentes.
    [HttpGet("backups")]
    public async Task<IActionResult> List(CancellationToken ct)
    {
        var (ok, adminId, reason) = await EnsureAdminAsync(ct);
        if (!ok) return StatusCode(403, new { error = reason });

        var r = await admin.ListBackupsAsync(adminId!.Value, HttpContext.Connection.RemoteIpAddress?.ToString(), ct);
        if (!r.Success) return StatusCode(500, new { error = r.Message });
        return Ok(new { respaldos = r.Data });
    }

    // Mantenimiento: limpia registros temporales obsoletos sin afectar pacientes.
    [HttpPost("maintenance")]
    public async Task<IActionResult> Maintenance(CancellationToken ct)
    {
        var (ok, adminId, reason) = await EnsureAdminAsync(ct);
        if (!ok) return StatusCode(403, new { error = reason });

        var r = await admin.RunMaintenanceAsync(adminId!.Value, HttpContext.Connection.RemoteIpAddress?.ToString(), ct);
        if (!r.Success) return StatusCode(500, new { error = r.Message });
        return Ok(new { mensaje = r.Message, detalle = r.Data });
    }

    // Descargar respaldo existente por id. Devuelve el archivo del disco local.
    [HttpGet("backups/{id}/download")]
    public async Task<IActionResult> Download(int id, CancellationToken ct)
    {
        var (ok, _, reason) = await EnsureAdminAsync(ct);
        if (!ok) return StatusCode(403, new { error = reason });

        var filePath = await admin.GetBackupFilePath(id, ct);
        if (filePath is null || !System.IO.File.Exists(filePath))
            return NotFound(new { error = "Archivo de respaldo no encontrado" });

        var fileName = Path.GetFileName(filePath);
        var contentType = Path.GetExtension(fileName).ToLowerInvariant() switch
        {
            ".json" => "application/json",
            _ => "application/octet-stream"
        };
        return PhysicalFile(filePath, contentType, fileName);
    }

    // Valida que el token corresponda a una sesión activa y el rol sea Administrador.
    private async Task<(bool Ok, int? AdminId, string Reason)> EnsureAdminAsync(CancellationToken ct)
    {
        var token = ReadBearerToken();
        var ip = HttpContext.Connection.RemoteIpAddress?.ToString();
        var adminId = await admin.GetAdminIdAsync(token, ip, ct);
        if (adminId is null)
        {
            if (string.IsNullOrWhiteSpace(token))
                return (false, null, "Tu sesión ha expirado. Inicia sesión nuevamente");
            return (false, null, "No tienes permisos para acceder a esta función");
        }
        return (true, adminId, string.Empty);
    }

    // Mismo parser robusto que AccountController (Authorization Bearer, comillas, ?token=).
    private string? ReadBearerToken()
    {
        var authHeader = "";
        if (Request.Headers.TryGetValue("Authorization", out var v1)) authHeader = v1.ToString();
        if (string.IsNullOrWhiteSpace(authHeader) && Request.Headers.TryGetValue("authorization", out var v2)) authHeader = v2.ToString();
        if (string.IsNullOrWhiteSpace(authHeader) && Request.Headers.TryGetValue("X-Auth-Token", out var v3)) authHeader = v3.ToString();

        if (!string.IsNullOrWhiteSpace(authHeader))
        {
            var parts = authHeader.Trim().Split(' ', StringSplitOptions.RemoveEmptyEntries);
            var token = parts.Length == 1 ? parts[0] : parts[^1];
            return token.Trim().Trim('"').Trim('\'');
        }

        if (Request.Query.TryGetValue("token", out var q))
            return q.ToString().Trim().Trim('"').Trim('\'');

        return null;
    }
}