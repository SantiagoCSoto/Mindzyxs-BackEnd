namespace Mindzyxs_API.Utilities;

public static class AppRoles
{
    public const string Paciente = "Paciente";
    public const string Psicologo = "Psicologo";
    public const string Administrador = "Administrador";
}

// Nombres exactos de dbo.estados (02_seed_catalogos.sql).
public static class AccountStates
{
    public const string Activa = "Activa";
    public const string Suspendida = "Suspendida";
    public const string Inactiva = "Inactiva";
    public const string PendienteVerificacion = "PendienteVerificacion";
    public const string PendienteAprobacion = "PendienteAprobacion";
    public const string Aprobado = "Aprobado";
}

public static class VerificationTypes
{
    public const string Registro = "Registro";
    public const string Recuperar = "Recuperar";
}

public static class LogAcciones
{
    public const string LoginAttempt = "LOGIN_ATTEMPT";
    public const string LoginSuccess = "LOGIN_SUCCESS";
    public const string LoginFailed = "LOGIN_FAILED";
    public const string LoginBloqueadoSuspendida = "LOGIN_BLOQUEADO_SUSPENDIDA";
    public const string LoginBloqueadoPendienteAprobacion = "LOGIN_BLOQUEADO_PENDIENTE_APROBACION";
    public const string SesionExpirada = "SESION_EXPIRADA";
    public const string PasswordReset = "PASSWORD_RESET";
    public const string Logout = "LOGOUT";
    public const string Backup = "BACKUP";
    public const string Restore = "RESTORE";
    public const string Mantenimiento = "MANTENIMIENTO";
    public const string VerRespaldos = "VIEW_BACKUPS";
    public const string PermisoDenegado = "PERMISO_DENEGADO";

    // PB-04 HU1-PB4: mensaje aclaratorio (visualización / error de carga).
    public const string AvisoAclaratorio = "AVISO_ACLARATORIO";
    public const string AvisoAclaratorioError = "AVISO_ACLARATORIO_ERROR";

    // PB-06 HU1-PB6: modo invitado (acceso, salida e intentos bloqueados sobre funciones exclusivas).
    public const string InvitadoAcceso = "INVITADO_ACCESO";
    public const string InvitadoAccesoError = "INVITADO_ACCESO_ERROR";
    public const string InvitadoBloqueo = "INVITADO_BLOQUEO";
    public const string InvitadoSalida = "INVITADO_SALIDA";
}
