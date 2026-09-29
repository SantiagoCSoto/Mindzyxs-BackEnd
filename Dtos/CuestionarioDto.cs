namespace Mindzyxs_API.Dtos;

/// <summary>Cuestionario pendiente para invitado: <c>GET /api/cuestionario/invitado</c> (PB-07 HU1-PB7).</summary>
public class CuestionarioInvitadoDto
{
    /// <summary>Indica si hay un cuestionario pendiente (false = todos respondidos).</summary>
    public bool Disponible { get; set; }

    /// <summary>Mensaje cuando no hay pendientes ("No tienes cuestionarios pendientes por responder.").</summary>
    public string? Mensaje { get; set; }

    /// <summary>Id del cuestionario pendiente.</summary>
    public int Id { get; set; }

    /// <summary>Título del cuestionario.</summary>
    public string Titulo { get; set; } = "";

    /// <summary>Descripción del cuestionario.</summary>
    public string? Descripcion { get; set; }

    /// <summary>Si el mensaje aclaratorio (no diagnóstico) es obligatorio antes de funciones sensibles.</summary>
    public bool AvisoObligatorio { get; set; } = true;

    /// <summary>Preguntas ordenadas (todas obligatorias).</summary>
    public List<PreguntaInvitadoDto> Preguntas { get; set; } = [];
}

/// <summary>Pregunta del cuestionario de invitado (siempre obligatoria).</summary>
public class PreguntaInvitadoDto
{
    /// <summary>Id de la pregunta.</summary>
    public int Id { get; set; }

    /// <summary>Texto de la pregunta.</summary>
    public string Texto { get; set; } = "";

    /// <summary>Orden de presentación.</summary>
    public int Orden { get; set; }

    /// <summary>Opciones de respuesta ordenadas (selección única).</summary>
    public List<OpcionInvitadoDto> Opciones { get; set; } = [];
}

/// <summary>Opción de respuesta de una pregunta.</summary>
public class OpcionInvitadoDto
{
    /// <summary>Id de la opción.</summary>
    public int Id { get; set; }

    /// <summary>Texto visible (ej. <c>A menudo</c>).</summary>
    public string Texto { get; set; } = "";

    /// <summary>Valor numérico para el puntaje.</summary>
    public int Valor { get; set; }

    /// <summary>Orden de presentación.</summary>
    public int Orden { get; set; }
}

/// <summary>Registro de inicio del cuestionario: <c>POST /api/cuestionario/invitado/inicio</c>.</summary>
public class InicioCuestionarioVm
{
    /// <summary>Id del cuestionario que el invitado va a responder.</summary>
    public int CuestionarioId { get; set; }
}

/// <summary>Una respuesta del invitado (pregunta → opción elegida).</summary>
public class RespuestaCuestionarioVm
{
    /// <summary>Id de la pregunta respondida.</summary>
    public int PreguntaId { get; set; }

    /// <summary>Id de la opción seleccionada.</summary>
    public int OpcionId { get; set; }
}

/// <summary>Finalización del cuestionario: <c>POST /api/cuestionario/invitado/finalizar</c>.</summary>
public class FinalizarCuestionarioVm
{
    /// <summary>Id del cuestionario finalizado.</summary>
    public int CuestionarioId { get; set; }

    /// <summary>Todas las respuestas (debe estar el 100% para finalizar).</summary>
    public List<RespuestaCuestionarioVm> Respuestas { get; set; } = [];
}

/// <summary>Resultado interno de la finalización (el controller agrega el mensaje).</summary>
public record FinalizarCuestionarioResult(int CuestionarioId, int Puntaje, string Nivel, int TotalPreguntas, int Respondidas);

/// <summary>Cuestionario pendiente para paciente: <c>GET /api/cuestionario/paciente/pendiente</c> (PB-11 HU1-PB11).</summary>
public class CuestionarioPacienteDto
{
    /// <summary>Indica si hay un cuestionario pendiente (false = todos respondidos).</summary>
    public bool Disponible { get; set; }

    /// <summary>Mensaje cuando no hay pendientes ("No tienes cuestionarios pendientes por responder.").</summary>
    public string? Mensaje { get; set; }

    /// <summary>Id del cuestionario pendiente.</summary>
    public int Id { get; set; }

    /// <summary>Título del cuestionario (título de la pantalla Cuestionario emocional).</summary>
    public string Titulo { get; set; } = "";

    /// <summary>Descripción del cuestionario.</summary>
    public string? Descripcion { get; set; }

    /// <summary>Si el mensaje aclaratorio (no diagnóstico) es obligatorio antes de iniciar.</summary>
    public bool AvisoObligatorio { get; set; } = true;

    /// <summary>Preguntas ordenadas (todas obligatorias). El front las muestra una por una.</summary>
    public List<PreguntaPacienteDto> Preguntas { get; set; } = [];

    /// <summary>Indica si el cuestionario es adaptativo (preguntas adicionales por condición). Hoy no
    /// hay soporte en BD: la estructura de preguntas es estática, el valor queda para el front.</summary>
    public bool Adaptativo { get; set; }
}

/// <summary>Pregunta del cuestionario del paciente (siempre obligatoria).</summary>
public class PreguntaPacienteDto
{
    /// <summary>Id de la pregunta.</summary>
    public int Id { get; set; }

    /// <summary>Texto de la pregunta.</summary>
    public string Texto { get; set; } = "";

    /// <summary>Orden de presentación.</summary>
    public int Orden { get; set; }

    /// <summary>Opciones de respuesta ordenadas (selección única).</summary>
    public List<OpcionPacienteDto> Opciones { get; set; } = [];
}

/// <summary>Opción de respuesta de una pregunta del paciente.</summary>
public class OpcionPacienteDto
{
    /// <summary>Id de la opción.</summary>
    public int Id { get; set; }

    /// <summary>Texto visible (ej. <c>A menudo</c>).</summary>
    public string Texto { get; set; } = "";

    /// <summary>Valor numérico para el puntaje.</summary>
    public int Valor { get; set; }

    /// <summary>Orden de presentación.</summary>
    public int Orden { get; set; }
}

/// <summary>Finalización del cuestionario del paciente: <c>POST /api/cuestionario/paciente/finalizar</c>.</summary>
public class FinalizarCuestionarioPacienteVm
{
    /// <summary>Id del cuestionario finalizado.</summary>
    public int CuestionarioId { get; set; }

    /// <summary>Todas las respuestas (debe estar el 100% para finalizar).</summary>
    public List<RespuestaCuestionarioVm> Respuestas { get; set; } = [];
}

/// <summary>Resultado interno de la finalización del paciente (persistida en su historial).</summary>
public record FinalizarCuestionarioPacienteResult(int IdEvento, int CuestionarioId, int Puntaje, string Nivel, int TotalPreguntas, int Respondidas);

/// <summary>Cuestionario asignado al paciente: <c>GET /api/cuestionario/paciente/asignados</c> (PB-22).</summary>
public class CuestionarioAsignadoDto
{
    /// <summary>Id de la asignación.</summary>
    public int IdAsignacion { get; set; }

    /// <summary>Id del cuestionario asignado.</summary>
    public int IdCuestionario { get; set; }

    /// <summary>Título del cuestionario.</summary>
    public string Titulo { get; set; } = string.Empty;

    /// <summary>Tipo del cuestionario (Predeterminado/Personalizado).</summary>
    public string Tipo { get; set; } = string.Empty;

    /// <summary>Fecha de asignación.</summary>
    public DateTime FechaAsignacion { get; set; }

    /// <summary>Fecha límite (puede ser nula).</summary>
    public DateTime? FechaLimite { get; set; }

    /// <summary>Estado de la asignación (Pendiente/Completada).</summary>
    public string Estado { get; set; } = string.Empty;
}
