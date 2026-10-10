using Microsoft.AspNetCore.Mvc;
using Mindzyxs_API.Dtos;
using Mindzyxs_API.Services;

namespace Mindzyxs_API.Controllers;

/// <summary>
/// Administración de BD (PB-27 HU1-PB27). Solo para rol Administrador con sesión activa.
/// Respaldos nativos SQL (.bak), listado, descarga y mantenimiento. Todo queda registrado en `dbo.auth`.
/// </summary>
[ApiController]
[Route("api/[controller]")]
public class AdminController(AdminService admin) : ControllerBase
{
    /// <summary>Crea un respaldo nativo de la base de datos (archivo `.bak`).</summary>
    /// <remarks>
    /// PB-27. Requiere `Authorization: Bearer &lt;token&gt;` de un Administrador.
    /// Genera un backup nativo de SQL Server (`Admin.BackupConnectionString`, usuario `mindzyxs_backup`)
    /// en `Admin.BackupsPath`, registra la fila en `dbo.respaldos` y un log en `dbo.auth`.
    /// Si el respaldo nativo falla, cae a un snapshot JSON como respaldo de contingencia (`Tipo: json`).
    ///
    /// Respuestas:
    /// - `201` — `{ "mensaje": "...", "respaldo": { "idRespaldo", "nombreArchivo", "tamano", "fechaHora", "ubicacion", "tipo" } }`
    /// - `403` — `{ "error": "Tu sesión ha expirado. Inicia sesión nuevamente" }` o `{ "error": "No tienes permisos para acceder a esta función" }`
    /// - `500` — `{ "error": "No fue posible crear el respaldo" }`
    /// </remarks>
    /// <response code="201">Respaldo creado correctamente.</response>
    /// <response code="403">Sesión inválida o sin rol Administrador.</response>
    /// <response code="500">Error interno al generar el respaldo.</response>
    [HttpPost("backup")]
    [ProducesResponseType(typeof(BackupCreatedResult), StatusCodes.Status201Created)]
    [ProducesResponseType(typeof(ErrorResult), StatusCodes.Status403Forbidden)]
    [ProducesResponseType(typeof(ErrorResult), StatusCodes.Status500InternalServerError)]
    public async Task<IActionResult> Backup(CancellationToken ct)
    {
        var (ok, adminId, reason) = await EnsureAdminAsync(ct);
        if (!ok) return StatusCode(403, new { error = reason });

        var r = await admin.CreateBackupAsync(adminId!.Value, HttpContext.Connection.RemoteIpAddress?.ToString(), ct);
        if (!r.Success) return StatusCode(500, new { error = r.Message });
        return StatusCode(201, new { mensaje = r.Message, respaldo = r.Data });
    }

    /// <summary>Lista los respaldos existentes de la base de datos.</summary>
    /// <remarks>
    /// PB-27. Requiere `Authorization: Bearer &lt;token&gt;` de un Administrador.
    /// Devuelve los respaldos registrados en `dbo.respaldos` (más recientes primero).
    ///
    /// Respuestas:
    /// - `200` — `{ "respaldos": [ { "idRespaldo", "nombreArchivo", "tamano", "fechaHora" }, ... ] }`
    /// - `403` — `{ "error": "Tu sesión ha expirado. Inicia sesión nuevamente" }` o `{ "error": "No tienes permisos para acceder a esta función" }`
    /// - `500` — `{ "error": "No fue posible consultar los respaldos" }`
    /// </remarks>
    /// <response code="200">Lista de respaldos registrados.</response>
    /// <response code="403">Sesión inválida o sin rol Administrador.</response>
    /// <response code="500">Error interno al consultar.</response>
    [HttpGet("backups")]
    [ProducesResponseType(typeof(BackupListResult), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ErrorResult), StatusCodes.Status403Forbidden)]
    [ProducesResponseType(typeof(ErrorResult), StatusCodes.Status500InternalServerError)]
    public async Task<IActionResult> List(CancellationToken ct)
    {
        var (ok, adminId, reason) = await EnsureAdminAsync(ct);
        if (!ok) return StatusCode(403, new { error = reason });

        var r = await admin.ListBackupsAsync(adminId!.Value, HttpContext.Connection.RemoteIpAddress?.ToString(), ct);
        if (!r.Success) return StatusCode(500, new { error = r.Message });
        return Ok(new { respaldos = r.Data });
    }

    /// <summary>Ejecuta el mantenimiento de la base de datos.</summary>
    /// <remarks>
    /// PB-27. Requiere `Authorization: Bearer &lt;token&gt;` de un Administrador.
    /// Limpia registros temporales obsoletos sin tocar datos de pacientes:
    /// códigos de verificación expirados, sesiones expiradas y logs de `dbo.auth` mayores a
    /// `Admin.RetencionLogDias` (90 días).
    ///
    /// Respuestas:
    /// - `200` — `{ "mensaje": "...", "detalle": { "codigosExpiradosEliminados", "sesionesExpiradasEliminadas", "logsAntiguosEliminados" } }`
    /// - `403` — `{ "error": "Tu sesión ha expirado. Inicia sesión nuevamente" }` o `{ "error": "No tienes permisos para acceder a esta función" }`
    /// - `500` — `{ "error": "No fue posible ejecutar el mantenimiento" }`
    /// </remarks>
    /// <response code="200">Mantenimiento ejecutado con detalle de registros limpiados.</response>
    /// <response code="403">Sesión inválida o sin rol Administrador.</response>
    /// <response code="500">Error interno al ejecutar el mantenimiento.</response>
    [HttpPost("maintenance")]
    [ProducesResponseType(typeof(MaintenanceResult), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ErrorResult), StatusCodes.Status403Forbidden)]
    [ProducesResponseType(typeof(ErrorResult), StatusCodes.Status500InternalServerError)]
    public async Task<IActionResult> Maintenance(CancellationToken ct)
    {
        var (ok, adminId, reason) = await EnsureAdminAsync(ct);
        if (!ok) return StatusCode(403, new { error = reason });

        var r = await admin.RunMaintenanceAsync(adminId!.Value, HttpContext.Connection.RemoteIpAddress?.ToString(), ct);
        if (!r.Success) return StatusCode(500, new { error = r.Message });
        return Ok(new { mensaje = r.Message, detalle = r.Data });
    }

    /// <summary>Descarga un respaldo existente por su id.</summary>
    /// <remarks>
    /// PB-27. Requiere `Authorization: Bearer &lt;token&gt;` de un Administrador.
    /// Devuelve el archivo físico del respaldo (`.bak` o `.json`) desde el disco local
    /// como descarga de archivo (`application/octet-stream` o `application/json`).
    ///
    /// Respuestas:
    /// - `200` — archivo binario del respaldo (Content-Disposition con el nombre del archivo)
    /// - `403` — `{ "error": "Tu sesión ha expirado. Inicia sesión nuevamente" }` o `{ "error": "No tienes permisos para acceder a esta función" }`
    /// - `404` — `{ "error": "Archivo de respaldo no encontrado" }`
    /// </remarks>
    /// <response code="200">Descarga del archivo de respaldo.</response>
    /// <response code="403">Sesión inválida o sin rol Administrador.</response>
    /// <response code="404">El respaldo no existe en disco.</response>
    [HttpGet("backups/{id}/download")]
    [ProducesResponseType(typeof(FileResult), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ErrorResult), StatusCodes.Status403Forbidden)]
    [ProducesResponseType(typeof(ErrorResult), StatusCodes.Status404NotFound)]
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

    /// <summary>Psicólogos con tarjeta o especialidades pendientes de verificación.</summary>
    /// <remarks>
    /// PB-25. Requiere `Authorization: Bearer &lt;token&gt;` de un Administrador.
    /// Cola de trabajo: tarjeta subida al cambiar/agregar especialidades.
    ///
    /// Respuestas:
    /// - `200` — `{ "psicologos": [ { "id", "nombre", "correo", "tarjetaVerificada", "especialidadesPendientes": [] } ] }`
    /// - `403` — sin rol Administrador.
    /// </remarks>
    /// <response code="200">Cola de verificación.</response>
    /// <response code="403">Solo administradores.</response>
    [HttpGet("psicologos/tarjeta-pendientes")]
    [ProducesResponseType(typeof(List<PsicologoTarjetaPendienteDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ErrorResult), StatusCodes.Status403Forbidden)]
    public async Task<IActionResult> TarjetaPendientes(CancellationToken ct)
    {
        var (ok, _, reason) = await EnsureAdminAsync(ct);
        if (!ok) return StatusCode(403, new { error = reason });

        var r = await admin.PendientesTarjetaAsync(ct);
        return Ok(new { psicologos = r.Data });
    }

    /// <summary>Verifica la tarjeta del psicólogo (sus pendientes se hacen visibles).</summary>
    /// <remarks>
    /// PB-25. Requiere `Authorization: Bearer &lt;token&gt;` de un Administrador.
    /// El psicólogo nunca pierde acceso: solo se restringe la visibilidad hasta aquí.
    ///
    /// Respuestas:
    /// - `200` — `{ "mensaje": "Tarjeta verificada correctamente. N especialidades visibles para los usuarios." }`
    /// - `403` — sin rol Administrador.
    /// - `404` — psicólogo inexistente o no es psicólogo.
    /// </remarks>
    /// <response code="200">Tarjeta verificada.</response>
    /// <response code="403">Solo administradores.</response>
    /// <response code="404">Psicólogo no válido.</response>
    [HttpPut("psicologos/{idPsicologo:int}/tarjeta-verificada")]
    [ProducesResponseType(typeof(MessageResult), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ErrorResult), StatusCodes.Status403Forbidden)]
    [ProducesResponseType(typeof(ErrorResult), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> VerificarTarjeta(int idPsicologo, CancellationToken ct)
    {
        var (ok, adminId, reason) = await EnsureAdminAsync(ct);
        if (!ok) return StatusCode(403, new { error = reason });

        var r = await admin.VerificarTarjetaAsync(adminId!.Value, idPsicologo, HttpContext.Connection.RemoteIpAddress?.ToString(), ct);
        if (!r.Success) return NotFound(new { error = r.Message });
        return Ok(new { mensaje = r.Message });
    }

    /// <summary>Solicitudes pendientes de validación de psicólogos.</summary>
    /// <remarks>
    /// PB-26 HU1-PB26 (fiel a v1 CU26). Requiere `Authorization: Bearer &lt;token&gt;` de un Administrador.
    /// Vacía → el front muestra `No hay solicitudes de validación pendientes.`
    ///
    /// Respuestas:
    /// - `200` — `{ "psicologos": [ { "id", "nombre", "correo", "fechaSolicitud" } ] }`
    /// - `403` — sin rol Administrador.
    /// </remarks>
    /// <response code="200">Solicitudes pendientes.</response>
    /// <response code="403">Solo administradores.</response>
    [HttpGet("psicologos/pendientes")]
    [ProducesResponseType(typeof(List<PsicologoPendienteDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ErrorResult), StatusCodes.Status403Forbidden)]
    public async Task<IActionResult> PendientesValidacion(CancellationToken ct)
    {
        var (ok, _, reason) = await EnsureAdminAsync(ct);
        if (!ok) return StatusCode(403, new { error = reason });

        var r = await admin.PendientesValidacionAsync(ct);
        return Ok(new { psicologos = r.Data });
    }

    /// <summary>Detalle de la solicitud (perfil + tarjeta + estado).</summary>
    /// <remarks>
    /// PB-26 HU1-PB26. Si ya fue procesada incluye `aviso: "La solicitud ya fue procesada."`.
    ///
    /// Respuestas:
    /// - `200` — detalle con `pendiente` y `aviso`.
    /// - `403` — sin rol Administrador.
    /// - `404` — psicólogo inexistente o no es psicólogo.
    /// </remarks>
    /// <response code="200">Detalle de la solicitud.</response>
    /// <response code="403">Solo administradores.</response>
    /// <response code="404">Psicólogo no válido.</response>
    [HttpGet("psicologos/{idPsicologo:int}/validacion")]
    [ProducesResponseType(typeof(ValidacionDetalleDto), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ErrorResult), StatusCodes.Status403Forbidden)]
    [ProducesResponseType(typeof(ErrorResult), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> DetalleValidacion(int idPsicologo, CancellationToken ct)
    {
        var (ok, _, reason) = await EnsureAdminAsync(ct);
        if (!ok) return StatusCode(403, new { error = reason });

        var r = await admin.DetalleValidacionAsync(idPsicologo, ct);
        if (!r.Success) return NotFound(new { error = r.Message });
        return Ok(r.Data);
    }

    /// <summary>Descarga el PDF de la tarjeta profesional.</summary>
    /// <remarks>
    /// PB-26 HU1-PB26.
    ///
    /// Respuestas:
    /// - `200` — archivo PDF.
    /// - `403` — sin rol Administrador.
    /// - `404` — `No se ha adjuntado tarjeta profesional.`
    /// </remarks>
    /// <response code="200">PDF de la tarjeta.</response>
    /// <response code="403">Solo administradores.</response>
    /// <response code="404">Sin tarjeta.</response>
    [HttpGet("psicologos/{idPsicologo:int}/tarjeta")]
    [ProducesResponseType(typeof(FileResult), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ErrorResult), StatusCodes.Status403Forbidden)]
    [ProducesResponseType(typeof(ErrorResult), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> DescargarTarjeta(int idPsicologo, CancellationToken ct)
    {
        var (ok, _, reason) = await EnsureAdminAsync(ct);
        if (!ok) return StatusCode(403, new { error = reason });

        var r = await admin.TarjetaPathAsync(idPsicologo, ct);
        if (!r.Success) return NotFound(new { error = r.Message });
        return PhysicalFile(r.Data!, "application/pdf", $"tarjeta_{idPsicologo}.pdf");
    }

    /// <summary>Aprueba al psicólogo (Aprobado/Activa).</summary>
    /// <remarks>
    /// PB-26 HU1-PB26. Solo si sigue pendiente, si no `La solicitud ya fue procesada.`
    /// Notifica por correo (no invalida si falla).
    ///
    /// Respuestas:
    /// - `200` — `{ "mensaje": "Validación registrada correctamente." }`
    /// - `403` — sin rol Administrador.
    /// - `404` — psicólogo inexistente / no es psicólogo / ya procesada.
    /// </remarks>
    /// <response code="200">Aprobado.</response>
    /// <response code="403">Solo administradores.</response>
    /// <response code="404">No procesable.</response>
    [HttpPost("psicologos/{idPsicologo:int}/aprobar")]
    [ProducesResponseType(typeof(MessageResult), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ErrorResult), StatusCodes.Status403Forbidden)]
    [ProducesResponseType(typeof(ErrorResult), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> AprobarPsicologo(int idPsicologo, CancellationToken ct)
    {
        var (ok, adminId, reason) = await EnsureAdminAsync(ct);
        if (!ok) return StatusCode(403, new { error = reason });

        var r = await admin.AprobarPsicologoAsync(adminId!.Value, idPsicologo, HttpContext.Connection.RemoteIpAddress?.ToString(), ct);
        if (!r.Success) return NotFound(new { error = r.Message });
        return Ok(new { mensaje = r.Message });
    }

    /// <summary>Rechaza al psicólogo (Rechazado/Inactiva, bloquea login).</summary>
    /// <remarks>
    /// PB-26 HU1-PB26. `motivo` opcional (va en el correo). Puede subir nueva
    /// tarjeta para ser evaluado nuevamente.
    ///
    /// Respuestas:
    /// - `200` — `{ "mensaje": "Validación registrada correctamente." }`
    /// - `403` — sin rol Administrador.
    /// - `404` — psicólogo inexistente / no es psicólogo / ya procesada.
    /// </remarks>
    /// <response code="200">Rechazado.</response>
    /// <response code="403">Solo administradores.</response>
    /// <response code="404">No procesable.</response>
    [HttpPost("psicologos/{idPsicologo:int}/rechazar")]
    [ProducesResponseType(typeof(MessageResult), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ErrorResult), StatusCodes.Status403Forbidden)]
    [ProducesResponseType(typeof(ErrorResult), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> RechazarPsicologo(int idPsicologo, [FromBody] RechazarPsicologoVm? vm, CancellationToken ct)
    {
        var (ok, adminId, reason) = await EnsureAdminAsync(ct);
        if (!ok) return StatusCode(403, new { error = reason });

        var r = await admin.RechazarPsicologoAsync(adminId!.Value, idPsicologo, vm?.Motivo, HttpContext.Connection.RemoteIpAddress?.ToString(), ct);
        if (!r.Success) return NotFound(new { error = r.Message });
        return Ok(new { mensaje = r.Message });
    }

    /// <summary>Valida que el token corresponda a una sesión activa y el rol sea Administrador.</summary>
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

    /// <summary>Solo `Authorization: Bearer &lt;token&gt;` (sin query ni headers alternos).</summary>
    private string? ReadBearerToken()
    {
        if (!Request.Headers.TryGetValue("Authorization", out var v)) return null;
        var authHeader = v.ToString().Trim();
        if (authHeader.StartsWith("Bearer ", StringComparison.OrdinalIgnoreCase))
            return authHeader["Bearer ".Length..].Trim().Trim('"').Trim('\'');
        return null;
    }
}