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
    ICatalogRepository catalogs,
    INotificacionRepository notificaciones,
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
            // Corrección BD: sp_mantenimiento_limpieza hace las 3 limpiezas en 1 llamada.
            var limpio = await admin.LimpiarTodoAsync(options.Value.RetencionLogDias, ct);

            var result = new MaintenanceResultDto
            {
                CodigosExpiradosEliminados = limpio.Codigos,
                SesionesExpiradasEliminadas = limpio.Sesiones,
                LogsAntiguosEliminados = limpio.Auth
            };

            await authLogs.LogAsync(adminId, adminId, LogAcciones.Mantenimiento,
                $"Mantenimiento completado: {limpio.Codigos} códigos, {limpio.Sesiones} sesiones, {limpio.Auth} logs", ip, ct);

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

    // PB-25: cola de tarjetas/especialidades pendientes de verificación.
    // El psicólogo NO pierde acceso: solo se ocultan hasta que el admin verifique.
    public async Task<OperationResult<List<PsicologoTarjetaPendienteDto>>> PendientesTarjetaAsync(CancellationToken ct = default)
    {
        var idRolPsi = await catalogs.GetRolIdAsync(AppRoles.Psicologo, ct);
        var filas = await users.GetPsicologosTarjetaPendienteAsync(idRolPsi, ct);
        var lista = new List<PsicologoTarjetaPendienteDto>();
        foreach (var f in filas)
        {
            lista.Add(new PsicologoTarjetaPendienteDto
            {
                Id = f.Id,
                Nombre = f.Nombre,
                Correo = f.Correo,
                TarjetaVerificada = f.TarjetaVerificada,
                EspecialidadesPendientes = await users.GetEspecialidadesPendientesAsync(f.Id, ct)
            });
        }
        return OperationResult<List<PsicologoTarjetaPendienteDto>>.Ok(lista);
    }

    // PB-25: verifica la tarjeta del psicólogo (las pendientes se hacen visibles).
    public async Task<OperationResult<string>> VerificarTarjetaAsync(int adminId, int idPsicologo, string? ip, CancellationToken ct = default)
    {
        var psi = await users.FindByIdAsync(idPsicologo, ct);
        if (psi is null)
            return OperationResult<string>.Fail("Psicólogo no encontrado.");
        var rol = await users.GetRolNombreAsync(psi.IdRol, ct);
        if (!string.Equals(rol, AppRoles.Psicologo, StringComparison.OrdinalIgnoreCase))
            return OperationResult<string>.Fail("El usuario no es psicólogo.");

        var n = await users.VerificarEspecialidadesAsync(idPsicologo, ct);
        await users.SetTarjetaVerificadaAsync(idPsicologo, true, ct);
        await authLogs.LogAsync(adminId, idPsicologo, LogAcciones.TarjetaVerificada,
            $"Admin:{adminId} verificó la tarjeta del psicólogo:{idPsicologo} ({n} especialidades visibles)", ip, ct);
        return new OperationResult<string> { Success = true, Message = $"Tarjeta verificada correctamente. {n} especialidades visibles para los usuarios." };
    }

    // PB-30: catálogo de notificaciones (6 eventos v1).
    public async Task<OperationResult<List<NotificacionDto>>> NotificacionesAsync(CancellationToken ct = default)
    {
        var filas = await notificaciones.GetAllAsync(ct);
        return OperationResult<List<NotificacionDto>>.Ok(filas.Select(f => new NotificacionDto
        {
            Tipo = f.Tipo,
            Nombre = NombreEvento(f.Tipo),
            Canales = (f.Canales ?? "correo").Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries).ToList(),
            Activo = f.Activo,
            Asunto = f.Asunto,
            Cuerpo = f.Cuerpo
        }).ToList());
    }

    // PB-30: detalle de un evento del catálogo.
    public async Task<OperationResult<NotificacionDto>> DetalleNotificacionAsync(string tipo, CancellationToken ct = default)
    {
        if (!TiposNotificacion.Todos.Contains(tipo, StringComparer.OrdinalIgnoreCase))
            return OperationResult<NotificacionDto>.Fail("Evento no encontrado.");
        var f = await notificaciones.FindByTipoAsync(NormalizarTipo(tipo), ct);
        if (f is null)
            return OperationResult<NotificacionDto>.Fail("Evento no encontrado.");
        return OperationResult<NotificacionDto>.Ok(new NotificacionDto
        {
            Tipo = f.Tipo,
            Nombre = NombreEvento(f.Tipo),
            Canales = (f.Canales ?? "correo").Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries).ToList(),
            Activo = f.Activo,
            Asunto = f.Asunto,
            Cuerpo = f.Cuerpo
        });
    }

    // PB-30: actualiza la config del evento (validaciones fieles a v1 CU31).
    public async Task<OperationResult<string>> ActualizarNotificacionAsync(int adminId, string tipo, ActualizarNotificacionVm vm, string? ip, CancellationToken ct = default)
    {
        if (!TiposNotificacion.Todos.Contains(tipo, StringComparer.OrdinalIgnoreCase))
            return OperationResult<string>.Fail("Evento no encontrado.");
        var canales = (vm.Canales ?? []).Select(c => c.Trim().ToLowerInvariant()).Where(c => c is "correo" or "sms" or "interna").Distinct().ToList();
        if (vm.Activo && canales.Count == 0)
            return OperationResult<string>.Fail("Debes habilitar al menos un canal para este evento.");
        if (vm.Activo && string.IsNullOrWhiteSpace(vm.Cuerpo))
            return OperationResult<string>.Fail("La plantilla no puede estar vacía.");
        if (vm.Activo && canales.Contains("correo") && string.IsNullOrWhiteSpace(vm.Asunto))
            return OperationResult<string>.Fail("El asunto es obligatorio para el canal correo.");

        await notificaciones.GuardarAsync(NormalizarTipo(tipo), string.Join(",", canales), vm.Activo,
            string.IsNullOrWhiteSpace(vm.Asunto) ? null : vm.Asunto.Trim(),
            string.IsNullOrWhiteSpace(vm.Cuerpo) ? null : vm.Cuerpo, adminId, ct);
        await authLogs.LogAsync(adminId, adminId, LogAcciones.NotificacionActualizada,
            $"Admin:{adminId} actualizó la notificación {NormalizarTipo(tipo)}", ip, ct);
        return new OperationResult<string> { Success = true, Message = "Notificación actualizada correctamente." };
    }

    // PB-30: preview con variables de ejemplo (no envía nada).
    public async Task<OperationResult<NotificacionDto>> PreviewNotificacionAsync(string tipo, PreviewNotificacionVm vm, CancellationToken ct = default)
    {
        if (!TiposNotificacion.Todos.Contains(tipo, StringComparer.OrdinalIgnoreCase))
            return OperationResult<NotificacionDto>.Fail("Evento no encontrado.");
        var f = await notificaciones.FindByTipoAsync(NormalizarTipo(tipo), ct);
        if (f is null)
            return OperationResult<NotificacionDto>.Fail("Evento no encontrado.");
        var vars = new Dictionary<string, string>
        {
            ["nombre"] = string.IsNullOrWhiteSpace(vm.Nombre) ? "María" : vm.Nombre.Trim(),
            ["codigo"] = string.IsNullOrWhiteSpace(vm.Codigo) ? "482913" : vm.Codigo.Trim(),
            ["fecha"] = string.IsNullOrWhiteSpace(vm.Fecha) ? DateTime.UtcNow.ToString("dd/MM/yyyy") : vm.Fecha.Trim(),
            ["motivo"] = string.IsNullOrWhiteSpace(vm.Motivo) ? "Tarjeta ilegible" : vm.Motivo.Trim()
        };
        return OperationResult<NotificacionDto>.Ok(new NotificacionDto
        {
            Tipo = f.Tipo,
            Nombre = NombreEvento(f.Tipo),
            Canales = (f.Canales ?? "correo").Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries).ToList(),
            Activo = f.Activo,
            Asunto = NotificationService.Sustituir(f.Asunto ?? "", vars),
            Cuerpo = NotificationService.Sustituir(f.Cuerpo ?? "", vars)
        });
    }

    private static string NormalizarTipo(string tipo)
        => TiposNotificacion.Todos.First(t => string.Equals(t, tipo, StringComparison.OrdinalIgnoreCase));

    private static string NombreEvento(string tipo) => tipo switch
    {
        TiposNotificacion.CodeSent => "Verificación de cuenta",
        TiposNotificacion.PasswordRecovery => "Recuperación de contraseña",
        TiposNotificacion.ApprovalGranted => "Aprobación de psicólogo",
        TiposNotificacion.ApprovalRejected => "Rechazo de psicólogo",
        TiposNotificacion.Reminder => "Recordatorio diario",
        TiposNotificacion.RiskHigh => "Alerta de riesgo alto",
        _ => tipo
    };
}
