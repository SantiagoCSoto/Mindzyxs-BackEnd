using Microsoft.Extensions.Options;
using Mindzyxs_API.Dtos;
using Mindzyxs_API.Repositories;
using Mindzyxs_API.Utilities;

namespace Mindzyxs_API.Services;

public class SesionOptions
{
    public int ExpiraHoras { get; set; } = 8;
}

public class LoginService(
    IUserRepository users,
    ICatalogRepository catalogs,
    ISesionRepository sesiones,
    IAuthLogRepository authLogs,
    IOptions<SesionOptions> sesionOptions,
    ILogger<LoginService> logger)
{
    public async Task<OperationResult<LoginResponseDto>> LoginAsync(string? correoRaw, string? passwordRaw, string? ip, CancellationToken ct = default)
    {
        var correo = ContactHelper.Normalize(correoRaw);
        var password = passwordRaw ?? string.Empty;

        // HU1-PB2 T2: validar vacíos y formato
        if (string.IsNullOrWhiteSpace(correo))
        {
            await authLogs.LogAsync(null, null, LogAcciones.LoginFailed, "Correo vacío", ip, ct);
            return OperationResult<LoginResponseDto>.Fail("Este campo es obligatorio");
        }
        if (!ContactHelper.IsEmail(correo))
        {
            await authLogs.LogAsync(null, null, LogAcciones.LoginFailed, $"Correo inválido: {correo}", ip, ct);
            return OperationResult<LoginResponseDto>.Fail("Ingresa un correo válido");
        }
        if (string.IsNullOrWhiteSpace(password))
        {
            await authLogs.LogAsync(null, null, LogAcciones.LoginFailed, "Contraseña vacía", ip, ct);
            return OperationResult<LoginResponseDto>.Fail("Este campo es obligatorio");
        }

        await authLogs.LogAsync(null, null, LogAcciones.LoginAttempt, $"Intento login: {correo}", ip, ct);

        var user = await users.FindByCorreoAsync(correo, ct);
        if (user is null)
        {
            await authLogs.LogAsync(null, null, LogAcciones.LoginFailed, $"Usuario no encontrado: {correo}", ip, ct);
            return OperationResult<LoginResponseDto>.Fail("Correo o contraseña incorrectos");
        }

        // Obtener nombres de estado y rol desde catálogos (toda la BD vive en Repositories)
        var estadoNombre = await users.GetEstadoCuentaNombreAsync(user.IdEstadoCuenta, ct)
                          ?? await catalogs.GetEstadoNombreAsync(user.IdEstadoCuenta, ct)
                          ?? string.Empty;
        var rolNombre = await users.GetRolNombreAsync(user.IdRol, ct)
                       ?? await catalogs.GetRolNombreAsync(user.IdRol, ct)
                       ?? string.Empty;

        // HU2-PB2: bloqueos por estado
        if (string.Equals(estadoNombre, AccountStates.Suspendida, StringComparison.OrdinalIgnoreCase)
            || string.Equals(estadoNombre, AccountStates.Inactiva, StringComparison.OrdinalIgnoreCase))
        {
            await authLogs.LogAsync(user.IdUsuario, user.IdUsuario, LogAcciones.LoginBloqueadoSuspendida,
                $"Bloqueado por estado {estadoNombre}: {correo}", ip, ct);
            return OperationResult<LoginResponseDto>.Fail("Tu cuenta se encuentra suspendida. Contacta al administrador");
        }

        if (string.Equals(estadoNombre, AccountStates.PendienteAprobacion, StringComparison.OrdinalIgnoreCase)
            && string.Equals(rolNombre, AppRoles.Psicologo, StringComparison.OrdinalIgnoreCase))
        {
            await authLogs.LogAsync(user.IdUsuario, user.IdUsuario, LogAcciones.LoginBloqueadoPendienteAprobacion,
                $"Bloqueado PendienteAprobacion psicólogo: {correo}", ip, ct);
            return OperationResult<LoginResponseDto>.Fail("Tu cuenta está pendiente de aprobación por el administrador. Intenta más tarde");
        }

        // Solo Activa puede continuar (bloquea PendienteVerificacion y demás)
        if (!string.Equals(estadoNombre, AccountStates.Activa, StringComparison.OrdinalIgnoreCase))
        {
            await authLogs.LogAsync(user.IdUsuario, user.IdUsuario, LogAcciones.LoginBloqueadoSuspendida,
                $"Bloqueado estado no activo {estadoNombre}: {correo}", ip, ct);
            // Si no es ninguno de los dos casos específicos, usar mensaje genérico de suspendida
            // (cumple HU2-PB2 para Suspendida/Inactiva; PendienteVerificacion queda bloqueado)
            return OperationResult<LoginResponseDto>.Fail("Tu cuenta se encuentra suspendida. Contacta al administrador");
        }

        // HU1-PB2 T3: validar password contra hash (PBKDF2 v1$...)
        if (!PasswordHelper.Verify(password, user.PasswordHash))
        {
            await authLogs.LogAsync(null, user.IdUsuario, LogAcciones.LoginFailed, $"Password incorrecto: {correo}", ip, ct);
            return OperationResult<LoginResponseDto>.Fail("Correo o contraseña incorrectos");
        }

        // HU1-PB2 T4: crear sesión (opaque token, hash en BD)
        var token = TokenHelper.Generate();
        var tokenHash = TokenHelper.Hash(token);
        var expiraEn = DateTime.UtcNow.AddHours(sesionOptions.Value.ExpiraHoras);

        try
        {
            await sesiones.CrearAsync(user.IdUsuario, tokenHash, expiraEn, ct);
            await users.UpdateUltimoAccesoAsync(user.IdUsuario, ct);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error creando sesión para {Correo}", correo);
            return OperationResult<LoginResponseDto>.Fail("No fue posible iniciar sesión. Intenta nuevamente");
        }

        await authLogs.LogAsync(user.IdUsuario, user.IdUsuario, LogAcciones.LoginSuccess, $"Login ok rol={rolNombre}", ip, ct);

        var panel = rolNombre switch
        {
            AppRoles.Paciente => "Paciente",
            AppRoles.Psicologo => "Psicologo",
            AppRoles.Administrador => "Administrador",
            _ => rolNombre
        };

        var resp = new LoginResponseDto
        {
            IdUsuario = user.IdUsuario,
            Correo = user.Correo,
            Rol = rolNombre,
            EstadoCuenta = estadoNombre,
            Token = token,
            ExpiraEn = expiraEn,
            Panel = panel
        };

        return OperationResult<LoginResponseDto>.Ok(resp, "Inicio de sesión exitoso");
    }

    public async Task<OperationResult<UsuarioDto>> ValidateSessionAsync(string? tokenRaw, string? ip, CancellationToken ct = default)
    {
        if (string.IsNullOrWhiteSpace(tokenRaw))
            return OperationResult<UsuarioDto>.Fail("Tu sesión ha expirado. Inicia sesión nuevamente");

        var hash = TokenHelper.Hash(tokenRaw.Trim());
        var sesion = await sesiones.FindByTokenHashAsync(hash, ct);
        if (sesion is null)
            return OperationResult<UsuarioDto>.Fail("Tu sesión ha expirado. Inicia sesión nuevamente");

        if (DateTime.UtcNow > sesion.ExpiraEn)
        {
            await sesiones.DeleteAsync(sesion.IdSesion, ct);
            await authLogs.LogAsync(sesion.IdUsuario, sesion.IdUsuario, LogAcciones.SesionExpirada, "Sesión expirada", ip, ct);
            return OperationResult<UsuarioDto>.Fail("Tu sesión ha expirado. Inicia sesión nuevamente");
        }

        var user = await users.FindByIdAsync(sesion.IdUsuario, ct);
        if (user is null)
            return OperationResult<UsuarioDto>.Fail("Tu sesión ha expirado. Inicia sesión nuevamente");

        var estadoNombre = await users.GetEstadoCuentaNombreAsync(user.IdEstadoCuenta, ct)
                          ?? await catalogs.GetEstadoNombreAsync(user.IdEstadoCuenta, ct)
                          ?? string.Empty;
        if (!string.Equals(estadoNombre, AccountStates.Activa, StringComparison.OrdinalIgnoreCase))
        {
            await sesiones.DeleteAsync(sesion.IdSesion, ct);
            await authLogs.LogAsync(user.IdUsuario, user.IdUsuario, LogAcciones.SesionExpirada, $"Sesión cerrada por estado {estadoNombre}", ip, ct);
            return OperationResult<UsuarioDto>.Fail("Tu cuenta se encuentra suspendida. Contacta al administrador");
        }

        await authLogs.LogAsync(user.IdUsuario, user.IdUsuario, "SESION_VALIDADA", "Sesión válida", ip, ct);
        return OperationResult<UsuarioDto>.Ok(user, "Sesión válida");
    }
}
