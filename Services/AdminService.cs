using Microsoft.Extensions.Options;
using Mindzyxs_API.Dtos;
using Mindzyxs_API.Repositories;
using Mindzyxs_API.Utilities;

namespace Mindzyxs_API.Services;

public class AdminOptions
{
    public string BackupsPath { get; set; } = @"C:\uploads\mindzyxs\backups";
    public int RetencionLogDias { get; set; } = 90;
    public string BackupConnectionString { get; set; } = string.Empty;
}

// PB-27 HU1-PB27: administrar BD. Crear respaldo local (.bak nativo de SQL Server o
// snapshot JSON de contingencia), listar/descargar respaldos y ejecutar mantenimiento
// de registros temporales obsoletos SIN tocar datos de pacientes. Todo queda en dbo.auth.
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
            var full = string.Empty;
            var nombre = string.Empty;
            var tipo = "bak";

            // 1) Respaldo nativo de SQL Server (.bak) con la credencial dedicada.
            try
            {
                nombre = $"mindzyx_full_{now:yyyy-MM-dd_HH-mm-ss}.bak";
                full = Path.Combine(root, nombre);
                await admin.CreateSqlBackupAsync(full, ct);
            }
            catch (Exception ex)
            {
                // 2) Contingencia: snapshot JSON si el .bak nativo falla (permisos/carpeta).
                logger.LogWarning(ex, "BACKUP DATABASE fallido por {AdminId}; usando snapshot JSON", adminId);
                tipo = "json";
                nombre = $"mindzyx_backup_{now:yyyy-MM-dd_HH-mm-ss}.backup";
                full = Path.Combine(root, nombre);
                var snapshot = await admin.ExportSnapshotJsonAsync(ct);
                await File.WriteAllTextAsync(full, snapshot, ct);
            }

            var bytes = new FileInfo(full).Length;
            var tamano = FormatSize(bytes);

            var idRespaldo = await admin.CrearRespaldoAsync(nombre, tamano, adminId, ct);
            await authLogs.LogAsync(adminId, adminId, LogAcciones.Backup,
                $"Respaldo {tipo} creado: {nombre} ({tamano}) en {root}", ip, ct);

            return OperationResult<BackupResultDto>.Ok(new BackupResultDto
            {
                IdRespaldo = idRespaldo,
                NombreArchivo = nombre,
                Tamano = tamano,
                FechaHora = now,
                Ubicacion = full,
                Tipo = tipo
            }, "Respaldo creado correctamente");
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "No fue posible crear el respaldo por {AdminId}", adminId);
            return OperationResult<BackupResultDto>.Fail("No fue posible crear el respaldo. Intente nuevamente");
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
                $"Mantenimiento completado: {cod} códigos, {ses} sesiones, {logs} logs", ip, ct);

            return OperationResult<MaintenanceResultDto>.Ok(result,
                "Mantenimiento completado — registros temporales eliminados (0 datos de pacientes afectados)");
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "No fue posible ejecutar mantenimiento por {AdminId}", adminId);
            return OperationResult<MaintenanceResultDto>.Fail("No fue posible ejecutar el mantenimiento. Intente nuevamente");
        }
    }

    // Devuelve la ruta física de un respaldo por id, o null si no existe.
    public async Task<string?> GetBackupFilePath(int idRespaldo, CancellationToken ct = default)
    {
        var r = await admin.FindRespaldoByIdAsync(idRespaldo, ct);
        if (r is null) return null;
        return Path.Combine(options.Value.BackupsPath, r.NombreArchivo);
    }

    // Devuelve el id_usuario del administrador autenticado o null si no es admin.
    public async Task<int?> GetAdminIdAsync(string? tokenRaw, string? ip, CancellationToken ct = default)
    {
        var sesion = await login.ValidateSessionAsync(tokenRaw, ip, ct);
        if (!sesion.Success || sesion.Data is null)
        {
            if (!string.IsNullOrWhiteSpace(tokenRaw))
                await authLogs.LogAsync(
                    sesion.Success ? sesion.Data!.IdUsuario : null, null,
                    LogAcciones.PermisoDenegado, "Acceso a Consola Administrador sin sesión válida", ip, ct);
            return null;
        }

        var rol = await users.GetRolNombreAsync(sesion.Data.IdRol, ct);
        if (!string.Equals(rol, AppRoles.Administrador, StringComparison.OrdinalIgnoreCase))
        {
            await authLogs.LogAsync(sesion.Data.IdUsuario, sesion.Data.IdUsuario,
                LogAcciones.PermisoDenegado, $"Acceso a Consola Administrador denegado (rol={rol})", ip, ct);
            return null;
        }
        return sesion.Data.IdUsuario;
    }

    private static string FormatSize(long bytes)
        => bytes >= 1024 * 1024 ? $"{bytes / (1024.0 * 1024):0.0} MB" : $"{bytes / 1024.0:0.0} KB";
}
