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
    IAuthLogRepository authLogs,
    LoginService login,
    PasswordRecoveryService recovery,
    IEmailSender email,
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

    // PB-25 tarjeta: verifica la tarjeta del psicólogo (las pendientes se hacen visibles).
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

    // PB-26 HU1-PB26: solicitudes pendientes de validación (fiel a v1 CU26).
    public async Task<OperationResult<List<PsicologoPendienteDto>>> PendientesValidacionAsync(CancellationToken ct = default)
    {
        var idRolPsi = await catalogs.GetRolIdAsync(AppRoles.Psicologo, ct);
        var idPend = await catalogs.GetEstadoIdAsync(AccountStates.PendienteAprobacion, ct);
        var filas = await users.GetPsicologosPendientesAsync(idRolPsi, idPend, ct);
        return OperationResult<List<PsicologoPendienteDto>>.Ok(filas
            .Select(f => new PsicologoPendienteDto { Id = f.Id, Nombre = f.Nombre, Correo = f.Correo, FechaSolicitud = f.FechaSolicitud })
            .ToList());
    }

    // PB-26 HU1-PB26: aprueba (Aprobado/Activa). Solo si sigue pendiente.
    public async Task<OperationResult<string>> AprobarPsicologoAsync(int adminId, int idPsicologo, string? ip, CancellationToken ct = default)
    {
        var psi = await users.FindByIdAsync(idPsicologo, ct);
        if (psi is null)
            return OperationResult<string>.Fail("Psicólogo no encontrado.");
        var rol = await users.GetRolNombreAsync(psi.IdRol, ct);
        if (!string.Equals(rol, AppRoles.Psicologo, StringComparison.OrdinalIgnoreCase))
            return OperationResult<string>.Fail("El usuario no es psicólogo.");

        var idPend = await catalogs.GetEstadoIdAsync(AccountStates.PendienteAprobacion, ct);
        var idAprob = await catalogs.GetEstadoIdAsync(AccountStates.Aprobado, ct);
        var idActiva = await catalogs.GetEstadoIdAsync(AccountStates.Activa, ct);
        if (!await users.AprobarPsicologoAsync(idPsicologo, idPend, idAprob, idActiva, ct))
            return OperationResult<string>.Fail("La solicitud ya fue procesada.");

        try
        {
            var nombre = $"{psi.Nombre} {psi.Apellido}".Trim();
            await email.SendAsync(psi.Correo, "Mindzyxs - Credenciales aprobadas",
                $"Hola {nombre}, sus credenciales profesionales han sido validadas. Su cuenta ha sido aprobada y ya puede iniciar sesión.");
        }
        catch { /* el correo no invalida la aprobación (fiel a v1) */ }

        await authLogs.LogAsync(adminId, idPsicologo, LogAcciones.ValidacionAprobada,
            $"Admin:{adminId} aprobó al psicólogo:{idPsicologo}", ip, ct);
        return new OperationResult<string> { Success = true, Message = "Validación registrada correctamente." };
    }

    // PB-26 HU1-PB26: detalle de la solicitud (perfil + tarjeta + estado).
    public async Task<OperationResult<ValidacionDetalleDto>> DetalleValidacionAsync(int idPsicologo, CancellationToken ct = default)
    {
        var psi = await users.FindByIdAsync(idPsicologo, ct);
        if (psi is null)
            return OperationResult<ValidacionDetalleDto>.Fail("Psicólogo no encontrado.");
        var rol = await users.GetRolNombreAsync(psi.IdRol, ct);
        if (!string.Equals(rol, AppRoles.Psicologo, StringComparison.OrdinalIgnoreCase))
            return OperationResult<ValidacionDetalleDto>.Fail("El usuario no es psicólogo.");

        var perfil = await users.FindPerfilProfesionalAsync(idPsicologo, ct);
        if (perfil is null)
            return OperationResult<ValidacionDetalleDto>.Fail("Psicólogo no encontrado.");

        var idPend = await catalogs.GetEstadoIdAsync(AccountStates.PendienteAprobacion, ct);
        var pendiente = perfil.IdEstadoAprobacion == idPend;
        var estadoAprobacion = perfil.IdEstadoAprobacion.HasValue
            ? await catalogs.GetEstadoNombreAsync(perfil.IdEstadoAprobacion.Value, ct) ?? ""
            : "";
        return OperationResult<ValidacionDetalleDto>.Ok(new ValidacionDetalleDto
        {
            Id = perfil.IdUsuario,
            Nombre = $"{perfil.Nombre} {perfil.Apellido}".Trim(),
            Correo = perfil.Correo,
            FechaSolicitud = perfil.FechaRegistro,
            Especialidades = perfil.Especialidades,
            TieneTarjeta = !string.IsNullOrWhiteSpace(perfil.FilePath),
            EstadoAprobacion = estadoAprobacion,
            Pendiente = pendiente,
            Aviso = pendiente ? "" : "La solicitud ya fue procesada."
        });
    }

    // PB-26 HU1-PB26: ruta del PDF de la tarjeta (null si no hay archivo).
    public async Task<OperationResult<string>> TarjetaPathAsync(int idPsicologo, CancellationToken ct = default)
    {
        var perfil = await users.FindPerfilProfesionalAsync(idPsicologo, ct);
        if (perfil is null || string.IsNullOrWhiteSpace(perfil.FilePath) || !File.Exists(perfil.FilePath))
            return OperationResult<string>.Fail("No se ha adjuntado tarjeta profesional.");
        return OperationResult<string>.Ok(perfil.FilePath);
    }

    // PB-26 HU1-PB26: rechaza (Rechazado/Inactiva, bloquea login). Motivo opcional.
    public async Task<OperationResult<string>> RechazarPsicologoAsync(int adminId, int idPsicologo, string? motivo, string? ip, CancellationToken ct = default)
    {
        var psi = await users.FindByIdAsync(idPsicologo, ct);
        if (psi is null)
            return OperationResult<string>.Fail("Psicólogo no encontrado.");
        var rol = await users.GetRolNombreAsync(psi.IdRol, ct);
        if (!string.Equals(rol, AppRoles.Psicologo, StringComparison.OrdinalIgnoreCase))
            return OperationResult<string>.Fail("El usuario no es psicólogo.");

        var idPend = await catalogs.GetEstadoIdAsync(AccountStates.PendienteAprobacion, ct);
        var idRech = await catalogs.GetEstadoIdAsync("Rechazado", ct);
        var idInac = await catalogs.GetEstadoIdAsync(AccountStates.Inactiva, ct);
        if (!await users.RechazarPsicologoAsync(idPsicologo, idPend, idRech, idInac, ct))
            return OperationResult<string>.Fail("La solicitud ya fue procesada.");

        try
        {
            var nombre = $"{psi.Nombre} {psi.Apellido}".Trim();
            var detalleMotivo = string.IsNullOrWhiteSpace(motivo) ? "No se proporcionó un motivo específico." : motivo.Trim();
            await email.SendAsync(psi.Correo, "Mindzyxs - Credenciales no aprobadas",
                $"Hola {nombre}, sus credenciales profesionales no fueron aprobadas. Motivo: {detalleMotivo} Puede subir una nueva tarjeta profesional para ser evaluado nuevamente.");
        }
        catch { /* el correo no invalida el rechazo (fiel a v1) */ }

        await authLogs.LogAsync(adminId, idPsicologo, LogAcciones.ValidacionRechazada,
            $"Admin:{adminId} rechazó al psicólogo:{idPsicologo}. Motivo: {(string.IsNullOrWhiteSpace(motivo) ? "-" : motivo.Trim())}", ip, ct);
        return new OperationResult<string> { Success = true, Message = "Validación registrada correctamente." };
    }

    // PB-25 HU1-PB25: lista de usuarios con filtros (fiel a v1 CU25, sin admins).
    public async Task<OperationResult<List<UsuarioAdminDto>>> UsuariosAsync(string? q, string? rol, string? estado, CancellationToken ct = default)
    {
        var idRolAdmin = await catalogs.GetRolIdAsync(AppRoles.Administrador, ct);
        int? idRol = null;
        if (!string.IsNullOrWhiteSpace(rol))
        {
            if (string.Equals(rol.Trim(), AppRoles.Administrador, StringComparison.OrdinalIgnoreCase))
                return OperationResult<List<UsuarioAdminDto>>.Fail("No puedes administrar usuarios administradores.");
            idRol = await catalogs.GetRolIdAsync(rol.Trim(), ct);
        }
        int? idEstado = null;
        if (!string.IsNullOrWhiteSpace(estado))
            idEstado = await catalogs.GetEstadoIdAsync(estado.Trim(), ct);

        var filas = await users.GetUsuariosAsync(idRolAdmin, q, idRol, idEstado, ct);
        return OperationResult<List<UsuarioAdminDto>>.Ok(filas
            .Select(f => new UsuarioAdminDto
            {
                Id = f.Id,
                Nombre = $"{f.Nombre} {f.Apellido}".Trim(),
                Correo = f.Correo,
                Rol = f.Rol,
                Estado = f.Estado,
                Aprobacion = f.Aprobacion,
                FechaRegistro = f.FechaRegistro
            }).ToList());
    }

    // PB-25 HU1-PB25: detalle del usuario (fiel a v1, sin password).
    public async Task<OperationResult<UsuarioDetalleDto>> DetalleUsuarioAsync(int idUsuario, CancellationToken ct = default)
    {
        var u = await users.FindByIdAsync(idUsuario, ct);
        if (u is null)
            return OperationResult<UsuarioDetalleDto>.Fail("Usuario no encontrado.");
        var rol = await users.GetRolNombreAsync(u.IdRol, ct) ?? "";
        if (string.Equals(rol, AppRoles.Administrador, StringComparison.OrdinalIgnoreCase))
            return OperationResult<UsuarioDetalleDto>.Fail("No puedes administrar usuarios administradores.");

        var generos = await catalogs.GetGenerosAsync(ct);
        return OperationResult<UsuarioDetalleDto>.Ok(new UsuarioDetalleDto
        {
            Id = u.IdUsuario,
            Nombres = u.Nombre,
            Apellidos = u.Apellido,
            Nombre = $"{u.Nombre} {u.Apellido}".Trim(),
            Correo = u.Correo,
            Rol = rol,
            Estado = await users.GetEstadoCuentaNombreAsync(u.IdEstadoCuenta, ct) ?? "",
            Aprobacion = u.IdEstadoAprobacion.HasValue ? await catalogs.GetEstadoNombreAsync(u.IdEstadoAprobacion.Value, ct) : null,
            FechaRegistro = u.FechaRegistro,
            FechaNacimiento = u.FechaNacimiento,
            Genero = u.IdGenero.HasValue ? generos.FirstOrDefault(g => g.Id == u.IdGenero.Value)?.Nombre : null
        });
    }

    // PB-25 HU1-PB25: edita datos básicos (fiel a v1 CU25).
    public async Task<OperationResult<string>> ActualizarUsuarioAsync(int adminId, int idUsuario, ActualizarUsuarioVm vm, string? ip, CancellationToken ct = default)
    {
        if (string.IsNullOrWhiteSpace(vm.Nombres))
            return OperationResult<string>.Fail("El nombre es obligatorio.");
        if (string.IsNullOrWhiteSpace(vm.Correo))
            return OperationResult<string>.Fail("El correo es obligatorio.");
        var correo = ContactHelper.Normalize(vm.Correo);
        if (!ContactHelper.IsEmail(correo))
            return OperationResult<string>.Fail("Ingresa un correo válido.");

        var u = await users.FindByIdAsync(idUsuario, ct);
        if (u is null)
            return OperationResult<string>.Fail("Usuario no encontrado.");
        var rol = await users.GetRolNombreAsync(u.IdRol, ct) ?? "";
        if (string.Equals(rol, AppRoles.Administrador, StringComparison.OrdinalIgnoreCase))
            return OperationResult<string>.Fail("No puedes administrar usuarios administradores.");

        var otro = await users.FindByCorreoAsync(correo, ct);
        if (otro is not null && otro.IdUsuario != idUsuario)
            return OperationResult<string>.Fail("El correo ya está registrado por otro usuario.");

        if (vm.IdGenero.HasValue && !await catalogs.ExisteGeneroAsync(vm.IdGenero.Value, ct))
            return OperationResult<string>.Fail("Género no válido.");

        await users.ActualizarPerfilAsync(idUsuario, vm.Nombres.Trim(), (vm.Apellidos ?? "").Trim(), vm.FechaNacimiento, vm.IdGenero, ct);
        if (!string.Equals(correo, u.Correo, StringComparison.OrdinalIgnoreCase))
            await users.ActualizarCorreoAsync(idUsuario, correo, ct);

        await authLogs.LogAsync(adminId, idUsuario, LogAcciones.UsuarioActualizado,
            $"Admin:{adminId} editó al usuario:{idUsuario}", ip, ct);
        return new OperationResult<string> { Success = true, Message = "Cambios guardados correctamente." };
    }

    // PB-25 HU1-PB25: cambia el estado de la cuenta (fiel a v1 CU25).
    public async Task<OperationResult<string>> CambiarEstadoAsync(int adminId, int idUsuario, string nuevoEstado, string? ip, CancellationToken ct = default)
    {
        if (string.IsNullOrWhiteSpace(nuevoEstado))
            return OperationResult<string>.Fail("Debes seleccionar un estado.");
        var u = await users.FindByIdAsync(idUsuario, ct);
        if (u is null)
            return OperationResult<string>.Fail("Usuario no encontrado.");
        var rol = await users.GetRolNombreAsync(u.IdRol, ct) ?? "";
        if (string.Equals(rol, AppRoles.Administrador, StringComparison.OrdinalIgnoreCase))
            return OperationResult<string>.Fail("No puedes administrar usuarios administradores.");

        int idEstado;
        try
        {
            idEstado = await catalogs.GetEstadoIdAsync(nuevoEstado.Trim(), ct);
        }
        catch
        {
            return OperationResult<string>.Fail("Estado no válido.");
        }

        await users.SetEstadoCuentaAsync(idUsuario, idEstado, ct);
        await authLogs.LogAsync(adminId, idUsuario, LogAcciones.UsuarioEstadoCambiado,
            $"Admin:{adminId} cambió el estado del usuario:{idUsuario} a {nuevoEstado.Trim()}", ip, ct);
        return new OperationResult<string> { Success = true, Message = "Estado actualizado correctamente." };
    }

    // PB-25 HU1-PB25: fuerza restablecimiento (código + correo, sin exponer nada).
    public async Task<OperationResult<string>> ForzarResetAsync(int adminId, int idUsuario, string? ip, CancellationToken ct = default)
    {
        var u = await users.FindByIdAsync(idUsuario, ct);
        if (u is null)
            return OperationResult<string>.Fail("Usuario no encontrado.");
        var rol = await users.GetRolNombreAsync(u.IdRol, ct) ?? "";
        if (string.Equals(rol, AppRoles.Administrador, StringComparison.OrdinalIgnoreCase))
            return OperationResult<string>.Fail("No puedes administrar usuarios administradores.");

        var r = await recovery.ForgotAsync(u.Correo, ct);
        if (!r.Success)
            return OperationResult<string>.Fail("No fue posible enviar el código. Intente nuevamente.");
        await authLogs.LogAsync(adminId, idUsuario, LogAcciones.PasswordForzado,
            $"Admin:{adminId} forzó restablecimiento para el usuario:{idUsuario}", ip, ct);
        return new OperationResult<string> { Success = true, Message = "Se envió un código de restablecimiento al correo del usuario." };
    }
}
