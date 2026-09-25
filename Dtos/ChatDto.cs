namespace Mindzyxs_API.Dtos;

/// <summary>Solicitud de chat: <c>POST /api/chat/solicitudes</c> (PB-31, solo psicólogo).</summary>
public class SolicitudChatVm
{
    /// <summary>Id del paciente al que se le ofrece el chat.</summary>
    public int IdPaciente { get; set; }
}

/// <summary>Contraparte de una conversación.</summary>
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

    /// <summary>Estado: Pendiente/Aceptada/Rechazada/Bloqueada/Cerrada.</summary>
    public string Estado { get; set; } = "";

    /// <summary>True si yo la bloqueé.</summary>
    public bool BloqueadaPorMi { get; set; }

    /// <summary>La otra persona.</summary>
    public ChatContraparteDto Contraparte { get; set; } = new();

    /// <summary>Fecha de creación.</summary>
    public DateTime FechaCreacion { get; set; }
}
