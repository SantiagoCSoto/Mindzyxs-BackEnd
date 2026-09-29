namespace Mindzyxs_API.Repositories.Models;

// Reflejan Mindzyxs-BD (01_create_tables_sqlserver.sql). Sin EDMX: POCOs EF Core.
public class RolRow
{
    public int IdRol { get; set; }
    public string Nombre { get; set; } = string.Empty;
}

public class GeneroRow
{
    public int IdGenero { get; set; }
    public string Nombre { get; set; } = string.Empty;
}

public class EstadoRow
{
    public int IdEstado { get; set; }
    public string Nombre { get; set; } = string.Empty;
}

public class UsuarioRow
{
    public int IdUsuario { get; set; }
    public string Nombre { get; set; } = string.Empty;
    public string Apellido { get; set; } = string.Empty;
    public string Correo { get; set; } = string.Empty;
    public string PasswordHash { get; set; } = string.Empty;
    public DateTime? FechaNacimiento { get; set; }
    public int IdRol { get; set; }
    public int IdEstadoCuenta { get; set; }
    public int? IdEstadoAprobacion { get; set; }
    public int? IdGenero { get; set; }
    public string? FilePath { get; set; }
    public DateTime FechaRegistro { get; set; }
    public DateTime? UltimoAcceso { get; set; }
}

public class CodigoRow
{
    public int IdCodigo { get; set; }
    public int IdUsuario { get; set; }
    public string Codigo { get; set; } = string.Empty;
    public string Tipo { get; set; } = string.Empty;
    public DateTime ExpiraEn { get; set; }
    public DateTime CreadoEn { get; set; }
}

public class SesionRow
{
    public int IdSesion { get; set; }
    public int IdUsuario { get; set; }
    public string TokenHash { get; set; } = string.Empty;
    public DateTime ExpiraEn { get; set; }
    public DateTime CreadaEn { get; set; }
}

public class AuthRow
{
    public int IdAuth { get; set; }
    public int? ActorIdUsuario { get; set; }
    public int? TargetIdUsuario { get; set; }
    public string Accion { get; set; } = string.Empty;
    public string? Detalle { get; set; }
    public DateTime Fecha { get; set; }
    public string? IpAddress { get; set; }
}

public class RespaldoRow
{
    public int IdRespaldo { get; set; }
    public string NombreArchivo { get; set; } = string.Empty;
    public string? Tamano { get; set; }
    public DateTime FechaHora { get; set; }
    public int IdAdmin { get; set; }
}

// PB-07: dbo.cuestionarios (Predeterminado/Publicado para invitados y pacientes;
// Personalizado + asignaciones_cuestionario para pacientes específicos).
public class CuestionarioRow
{
    public int IdCuestionario { get; set; }
    public string Titulo { get; set; } = string.Empty;
    public string? Descripcion { get; set; }
    public string Tipo { get; set; } = string.Empty;
    public string Estado { get; set; } = string.Empty;
    public int IdCreador { get; set; }
    public DateTime FechaCreacion { get; set; }
}

// PB-22: dbo.asignaciones_cuestionario (el psicólogo asigna cuestionarios
// a pacientes específicos; el crear-asignación llega en sprint 4).
public class AsignacionRow
{
    public int IdAsignacion { get; set; }
    public int IdCuestionario { get; set; }
    public int IdPaciente { get; set; }
    public int IdPsicologo { get; set; }
    public DateTime FechaAsignacion { get; set; }
    public DateTime? FechaLimite { get; set; }
    public string Estado { get; set; } = string.Empty;
}

// PB-especialidades: dbo.especialidades (checklist de 100) +
// dbo.psicologo_especialidades (una, varias o ninguna por psicólogo).
public class EspecialidadRow
{
    public int IdEspecialidad { get; set; }
    public string Nombre { get; set; } = string.Empty;
    public string? Descripcion { get; set; }
}

/// <summary>Fila de <c>dbo.psicologo_especialidades</c> (PK compuesta).</summary>
public class PsicologoEspecialidadRow
{
    public int IdPsicologo { get; set; }
    public int IdEspecialidad { get; set; }
}

// PB-07: dbo.preguntas. Todas son siempre obligatorias (sin flag).
// tipo: 'escala' (entra al promedio de puntaje) u otros ('binaria', 'opcion_multiple').
public class PreguntaRow
{
    public int IdPregunta { get; set; }
    public int IdCuestionario { get; set; }
    public string Texto { get; set; } = string.Empty;
    public int Orden { get; set; }
    public string? Tipo { get; set; }
    public int? PuntajeMax { get; set; }
}

// PB-07: dbo.opciones_pregunta (texto, valor numérico para el puntaje, orden).
public class OpcionPreguntaRow
{
    public int IdOpcion { get; set; }
    public int IdPregunta { get; set; }
    public string Texto { get; set; } = string.Empty;
    public int Valor { get; set; }
    public int Orden { get; set; }
}

// PB-08: dbo.lineas_ayuda (solo se sirven las Activas a invitados y pacientes).
// Directorio nacional + territorial (Minsalud ago-2025): canales, tipo de
// atención, territorio, fuente oficial y fecha de verificación.
public class LineaAyudaRow
{
    public int IdLinea { get; set; }
    public string Nombre { get; set; } = string.Empty;
    public string? Telefono { get; set; }
    public string? Url { get; set; }
    public string? Horario { get; set; }
    public string? Cobertura { get; set; }
    public string Estado { get; set; } = string.Empty;
    public string? Descripcion { get; set; }
    public string? Whatsapp { get; set; }
    public string? Email { get; set; }
    public string? ChatUrl { get; set; }
    public string? Videollamada { get; set; }
    public string? TipoAtencion { get; set; }
    public string? Departamento { get; set; }
    public string? Municipio { get; set; }
    public string? Poblacion { get; set; }
    public string? Fuente { get; set; }
    public DateOnly? FechaVerificacion { get; set; }
}

// PB-09: dbo.recomendaciones (autocuidado básico; nivel_riesgo NULL = transversal).
// Contenido administrable por el admin (PB-28). Detalle: intro, pasos (TEXT con
// saltos de línea), duración, instrucciones e icono (NVARCHAR, emoji).
public class RecomendacionRow
{
    public int IdRecomendacion { get; set; }
    public string Titulo { get; set; } = string.Empty;
    public string Contenido { get; set; } = string.Empty;
    public string? NivelRiesgo { get; set; }
    public string? Intro { get; set; }
    public string? Pasos { get; set; }
    public string? Duracion { get; set; }
    public string? Instrucciones { get; set; }
    public string? Icono { get; set; }
}

// PB-31: dbo.conversaciones (solicitud/aceptación/rechazo/bloqueo en SQL;
// los mensajes en vivo van por Firebase RTDB). Una fila por pareja
// (UQ_conv_pareja): rechazadas/cerradas se reactivan con UPDATE.
public class ConversacionRow
{
    public int IdConversacion { get; set; }
    public int IdPaciente { get; set; }
    public int IdPsicologo { get; set; }
    public string Estado { get; set; } = string.Empty;
    public int? IdBloqueador { get; set; }
    public DateTime FechaCreacion { get; set; }
    public DateTime? FechaRespuesta { get; set; }
}

// PB-13: dbo.eventos_bienestar (registros y cuestionarios del paciente).
// nota = descripción propia de cómo se sintió (máx. 200 palabras, igual que v1).
// PB-31 la reutiliza para el último riesgo del buscador.
public class EventoRow
{
    public int IdEvento { get; set; }
    public int IdPaciente { get; set; }
    public int IdTipoEvento { get; set; }
    public int? IdCuestionario { get; set; }
    public int? IdNivelRiesgo { get; set; }
    public int? PuntajeTotal { get; set; }
    public DateTime FechaEvento { get; set; }
    public string? Nota { get; set; }
}

// PB-13: dbo.respuestas (valor + texto por pregunta de un evento).
public class RespuestaRow
{
    public int IdRespuesta { get; set; }
    public int IdEvento { get; set; }
    public int IdPregunta { get; set; }
    public int? Valor { get; set; }
    public string? Texto { get; set; }
    public DateTime FechaRespuesta { get; set; }
}

// PB-13/19: dbo.observaciones (apoyo del psicólogo sobre un evento/paciente).
public class ObservacionRow
{
    public int IdObservacion { get; set; }
    public int IdPsicologo { get; set; }
    public int IdPaciente { get; set; }
    public int? IdEvento { get; set; }
    public string Contenido { get; set; } = string.Empty;
    public string? Firma { get; set; }
    public DateTime FechaObservacion { get; set; }
}

// PB-14: dbo.autorizaciones (paciente autoriza a psicólogo; Autorizado = id 8).
// PB-31 la reutiliza para validar el chat.
public class AutorizacionRow
{
    public int IdAutorizacion { get; set; }
    public int IdPaciente { get; set; }
    public int IdPsicologo { get; set; }
    public int IdEstado { get; set; }
    public DateTime FechaSolicitud { get; set; }
    public DateTime? FechaRespuesta { get; set; }
}

// PB-13: dbo.niveles_riesgo (Bajo/Medio/Alto).
public class NivelRiesgoRow
{
    public int IdNivelRiesgo { get; set; }
    public string Nombre { get; set; } = string.Empty;
}

// PB-15: dbo.recordatorios (una config por paciente; Diaria = todos los días
// implícitos, Semanal usa dbo.recordatorio_dias con id_dia 1=Lunes..7=Domingo).
public class RecordatorioRow
{
    public int IdRecordatorio { get; set; }
    public int IdPaciente { get; set; }
    public string Titulo { get; set; } = string.Empty;
    public string? Mensaje { get; set; }
    public TimeSpan Hora { get; set; }
    public bool Activo { get; set; }
    public string Frecuencia { get; set; } = "Diaria";
}

// PB-15: dbo.recordatorio_dias (días 1=Lunes..7=Domingo para frecuencia Semanal).
public class RecordatorioDiaRow
{
    public int IdRecordatorio { get; set; }
    public int IdDia { get; set; }
}

// PB-31 (solo lectura): tipos de evento (RegistroEmocional, Cuestionario).
public class TipoEventoRow
{
    public int IdTipoEvento { get; set; }
    public string Nombre { get; set; } = string.Empty;
}
