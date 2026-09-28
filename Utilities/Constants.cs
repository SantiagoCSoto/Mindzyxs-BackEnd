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
    public const string CambioCorreo = "CambioCorreo";
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

    // PB-07 HU1-PB7: cuestionario emocional del invitado (inicio y finalización al 100%).
    public const string CuestionarioInvitadoInicio = "CUESTIONARIO_INVITADO_INICIO";
    public const string CuestionarioInvitadoFin = "CUESTIONARIO_INVITADO_FIN";

    // PB-11 HU1-PB11: cuestionario emocional del paciente autenticado (inicio y
    // finalización con persistencia de respuestas y resultado en su historial).
    public const string CuestionarioPacienteInicio = "CUESTIONARIO_PACIENTE_INICIO";
    public const string CuestionarioPacienteFin = "CUESTIONARIO_PACIENTE_FIN";

    // PB-12 HU1-PB12: registro del estado emocional del paciente (escala 1-5 + nota opcional).
    public const string RegistroEmocionalCreado = "REGISTRO_EMOCIONAL_CREADO";

    // PB-08 HU1-PB8: cálculo y visualización del riesgo emocional del invitado.
    public const string RiesgoInvitadoCalculo = "RIESGO_INVITADO_CALCULO";

    // PB-09 HU1-PB9: consulta de recomendaciones básicas del invitado (lista y detalle).
    public const string RecomendacionInvitadoConsulta = "RECOMENDACION_INVITADO_CONSULTA";

    // PB-31 HU1-PB31: ciclo de vida del chat (solicitudes, mensajes, bloqueos).
    public const string ChatSolicitud = "CHAT_SOLICITUD";
    public const string ChatAceptado = "CHAT_ACEPTADO";
    public const string ChatRechazado = "CHAT_RECHAZADO";
    public const string ChatBloqueo = "CHAT_BLOQUEO";
    public const string ChatDesbloqueo = "CHAT_DESBLOQUEO";
    public const string ChatMensaje = "CHAT_MENSAJE";

    // Corrección BD: acciones registradas por triggers (ver sql/10_triggers_auth.sql).
    public const string CambioEstadoCuenta = "CAMBIO_ESTADO_CUENTA";
    public const string SesionCreada = "SESION_CREADA";

    // PB-13 HU1-PB13: consulta del historial emocional (lista y detalle).
    public const string HistorialConsulta = "HISTORIAL_CONSULTA";

    // PB-14 HU1-PB14: consulta del progreso de pacientes autorizados.
    public const string ProgresoConsulta = "PROGRESO_CONSULTA";

    // PB-15 HU1-PB15: configuración de recordatorios.
    public const string RecordatorioConfig = "RECORDATORIO_CONFIG";

    // PB-10 HU1-PB10: edición de perfil y cambio de correo.
    public const string PerfilActualizado = "PERFIL_ACTUALIZADO";
    public const string CorreoSolicitado = "CORREO_SOLICITADO";
    public const string CorreoActualizado = "CORREO_ACTUALIZADO";
}
