namespace Mindzyxs_API.Dtos;

/// <summary>Solicitud de chat: <c>POST /api/chat/solicitudes</c> (PB-31, solo psicólogo).</summary>
public class SolicitudChatVm
{
    /// <summary>Id del paciente al que se le ofrece el chat.</summary>
    public int IdPaciente { get; set; }
}

/// <summary>Solicitud de chat del paciente: <c>POST /api/chat/solicitudes/paciente</c> (doble vía).</summary>
public class SolicitudPacienteVm
{
    /// <summary>Id del psicólogo al que se le pide el chat.</summary>
    public int IdPsicologo { get; set; }
}

/// <summary>Solicitud de bloqueo: <c>POST /api/chat/conversaciones/{id}/bloqueo/solicitar</c>.</summary>
public class SolicitudBloqueoVm
{
    /// <summary>Motivo (obligatorio, máx. 500).</summary>
    public string? Motivo { get; set; }
}

/// <summary>Resolver bloqueo: <c>POST /api/chat/conversaciones/{id}/bloqueo/resolver</c>.</summary>
public class ResolverBloqueoVm
{
    /// <summary>True confirma el bloqueo, false lo rechaza (vuelve a Aceptada).</summary>
    public bool Aceptar { get; set; }
}

/// <summary>Paciente en el buscador: <c>GET /api/chat/pacientes?q=</c> (solo psicólogo).</summary>
public class PacienteChatDto
{    /// <summary>Id del paciente (para ofrecer chat).</summary>
    public int Id { get; set; }

    /// <summary>Nombre visible.</summary>
    public string Nombre { get; set; } = "";

    /// <summary>Correo (para distinguir homónimos).</summary>
    public string Correo { get; set; } = "";

    /// <summary>Último puntaje 1-5 (null = sin datos).</summary>
    public int? UltimoPuntaje { get; set; }

    /// <summary>Último nivel (Bajo/Medio/Alto, null = sin datos).</summary>
    public string? UltimoNivel { get; set; }

    /// <summary>Fecha del último registro.</summary>
    public DateTime? UltimaFecha { get; set; }

    /// <summary>Cuestionarios respondidos (eventos tipo Cuestionario).</summary>
    public int CuestionariosRespondidos { get; set; }

    /// <summary>Psicólogos autorizados vinculados.</summary>
    public int Psicologos { get; set; }
}
public class ChatContraparteDto
{
    /// <summary>Id del otro usuario.</summary>
    public int Id { get; set; }

    /// <summary>Nombre visible.</summary>
    public string Nombre { get; set; } = "";

    /// <summary>Rol (Paciente/Psicologo).</summary>
    public string Rol { get; set; } = "";
}

/// <summary>Conversación del usuario: <c>GET /api/chat/conversaciones</c>.</summary>
public class ConversacionDto
{
    /// <summary>Id de la conversación (coincide con RTDB /conversaciones/{id}).</summary>
    public int Id { get; set; }

    /// <summary>Estado: Pendiente/Aceptada/Rechazada/Bloqueada/BloqueoPendiente/Cerrada.</summary>
    public string Estado { get; set; } = "";

    /// <summary>Quién pidió el chat (id de paciente o psicólogo).</summary>
    public int? IdSolicitante { get; set; }

    /// <summary>Motivo de la solicitud de bloqueo (si hay).</summary>
    public string? MotivoBloqueo { get; set; }

    /// <summary>True si yo la bloqueé.</summary>
    public bool BloqueadaPorMi { get; set; }

    /// <summary>La otra persona.</summary>
    public ChatContraparteDto Contraparte { get; set; } = new();

    /// <summary>Fecha de creación.</summary>
    public DateTime FechaCreacion { get; set; }
}
