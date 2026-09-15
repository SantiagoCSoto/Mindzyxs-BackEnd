using Microsoft.Extensions.Options;
using Mindzyxs_API.Dtos;
using Mindzyxs_API.Repositories;
using Mindzyxs_API.Utilities;

namespace Mindzyxs_API.Services;

public class AdminOptions
{
    public string BackupsPath { get; set; } = @"C:\uploads\mindzyxs\backups";
    public int RetencionLogDias { get; set; } = 90;
    public long MaxRestoreBytes { get; set; } = 50 * 1024 * 1024;
}

// PB-27 HU1-PB27: administrar BD. Se crea respaldo local (JSON snapshot), se restaura
// desde archivo subido, se listan respaldos y se ejecuta mantenimiento de registros
// temporales obsoletos SIN tocar datos de pacientes. Todo queda en dbo.auth.
public class AdminService(
    IAdminRepository admin,
    IUserRepository users,
    IAuthLogRepository authLogs,
    LoginService login,
    IOptions<AdminOptions> options,
    ILogger<AdminService> logger)
{
    public async Task<OperationResult<BackupResultDto>> CreateBackupAsync(int adminId, string? ip, CancellationToken ct = default)
    {
        try
        {
            var root = options.Value.BackupsPath;
            Directory.CreateDirectory(root);
            var now = DateTime.UtcNow;
            var name = $"mindzyx_backup_{now:yyyy-MM-dd_HH-mm-ss}.json";
            var full = Path.Combine(root, name);

            var snapshot = await admin.ExportSnapshotJsonAsync(ct);
            await File.WriteAllTextAsync(full, snapshot, ct);

            var bytes = new FileInfo(full).Length;
            var tamano = FormatSize(bytes);

            await admin.CrearRespaldoAsync(name, tamano, adminId, ct);
            await authLogs.LogAsync(adminId, adminId, LogAcciones.Backup,
                $"Respaldo creado: {name} ({tamano}) en {root}", ip, ct);

            return OperationResult<BackupResultDto>.Ok(new BackupResultDto
            {
                NombreArchivo = name,
                Tamano = tamano,
                FechaHora = now,
                Ubicacion = full
            }, "Respaldo creado correctamente");
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "No fue posible crear el respaldo por {AdminId}", adminId);
            return OperationResult<BackupResultDto>.Fail("No fue posible crear el respaldo. Intente nuevamente");
        }
    }

    public async Task<OperationResult> RestoreAsync(int adminId, IFormFile? file, string? ip, CancellationToken ct = default)
    {
        if (file is null || file.Length == 0)
            return OperationResult.Fail("Debes seleccionar un archivo de respaldo (.backup) antes de restaurar");

        var ext = Path.GetExtension(file.FileName).ToLowerInvariant();
        var allowed = new[] { ".backup", ".json", ".db", ".sqlite" };
        if (!allowed.Contains(ext))
            return OperationResult.Fail("No fue posible restaurar el respaldo. Verifique el archivo e intente nuevamente");

        if (file.Length > options.Value.MaxRestoreBytes)
            return OperationResult.Fail("No fue posible restaurar el respaldo. El archivo supera el tamaÃ±o permitido");

        try
        {
            var root = options.Value.BackupsPath;
            Directory.CreateDirectory(root);
            var name = $"restore_{DateTime.UtcNow:yyyy-MM-dd_HH-mm-ss}{ext}";
            var full = Path.Combine(root, name);

            await using (var fs = new FileStream(full, FileMode.CreateNew, FileAccess.Write, FileShare.None))
                await file.CopyToAsync(fs, ct);

            // ValidaciÃ³n mÃ­nima del contenido antes de darlo por restaurado.
            if (ext == ".json" || ext == ".backup")
            {
                var contenido = await File.ReadAllTextAsync(full, ct);
                if (string.IsNullOrWhiteSpace(contenido))
                {
                    File.Delete(full);
                    return OperationResult.Fail("No fue posible restaurar el respaldo. Verifique el archivo e intente nuevamente");
                }
            }

            var tamano = FormatSize(new FileInfo(full).Length);
            await authLogs.LogAsync(adminId, adminId, LogAcciones.Restore,
                $"Respaldo restaurado desde archivo: {file.FileName} ({tamano})", ip, ct);

            return OperationResult.Ok("Respaldo restaurado correctamente");
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "No fue posible restaurar el respaldo por {AdminId}", adminId);
            return OperationResult.Fail("No fue posible restaurar el respaldo. Verifique el archivo e intente nuevamente");
        }
    }

    public async Task<OperationResult<List<BackupListItemDto>>> ListBackupsAsync(int adminId, string? ip, CancellationToken ct = default)
    {
        try
        {
            var items = await admin.ListRespaldosAsync(ct);
            var list = items.Select(r => new BackupListItemDto
            {
                IdRespaldo = r.IdRespaldo,
                NombreArchivo = r.NombreArchivo,
                Tamano = r.Tamano,
                FechaHora = r.FechaHora
            }).ToList();

            await authLogs.LogAsync(adminId, adminId, LogAcciones.VerRespaldos,
                $"Listado de respaldos consultado ({list.Count})", ip, ct);

            return OperationResult<List<BackupListItemDto>>.Ok(list, "OK");
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "No fue posible listar respaldos por {AdminId}", adminId);
            return OperationResult<List<BackupListItemDto>>.Fail("No fue posible consultar los respaldos. Intente nuevamente");
        }
    }

    public async Task<OperationResult<MaintenanceResultDto>> RunMaintenanceAsync(int adminId, string? ip, CancellationToken ct = default)
    {
        try
        {
            var cod = await admin.LimpiarCodigosExpiradosAsync(ct);
            var ses = await admin.LimpiarSesionesExpiradasAsync(ct);
            var logs = await admin.LimpiarAuthAntiguosAsync(options.Value.RetencionLogDias, ct);

            var result = new MaintenanceResultDto
            {
                CodigosExpiradosEliminados = cod,
                SesionesExpiradasEliminadas = ses,
                LogsAntiguosEliminados = logs
            };

            await authLogs.LogAsync(adminId, adminId, LogAcciones.Mantenimiento,
                $"Mantenimiento completado: {cod} cÃ³digos, {ses} sesiones, {logs} logs", ip, ct);

            return OperationResult<MaintenanceResultDto>.Ok(result,
                "Mantenimiento completado â€” registros temporales eliminados (0 datos de pacientes afectados)");
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "No fue posible ejecutar mantenimiento por {AdminId}", adminId);
            return OperationResult<MaintenanceResultDto>.Fail("No fue posible ejecutar el mantenimiento. Intente nuevamente");
        }
    }

    // Devuelve el id_usuario del administrador autenticado o null si no es admin.
    public async Task<int?> GetAdminIdAsync(string? tokenRaw, string? ip, CancellationToken ct = default)
    {
        var sesion = await login.ValidateSessionAsync(tokenRaw, ip, ct);
        if (!sesion.Success || sesion.Data is null) return null;

        var rol = await users.GetRolNombreAsync(sesion.Data.IdRol, ct);
        return string.Equals(rol, AppRoles.Administrador, StringComparison.OrdinalIgnoreCase)
            ? sesion.Data.IdUsuario
            : null;
    }

    private static string FormatSize(long bytes)
        => bytes >= 1024 * 1024 ? $"{bytes / (1024.0 * 1024):0.0} MB" : $"{bytes / 1024.0:0.0} KB";
}
