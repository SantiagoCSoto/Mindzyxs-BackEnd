namespace Mindzyxs_API.Dtos;

/// <summary>Solicitud pendiente de validación: <c>GET /api/admin/psicologos/pendientes</c> (PB-26 HU1-PB26).</summary>
public class PsicologoPendienteDto
{
    /// <summary>Id del psicólogo.</summary>
    public int Id { get; set; }

    /// <summary>Nombre completo.</summary>
    public string Nombre { get; set; } = string.Empty;

    /// <summary>Correo.</summary>
    public string Correo { get; set; } = string.Empty;

    /// <summary>Fecha de solicitud (registro).</summary>
    public DateTime FechaSolicitud { get; set; }
}

/// <summary>Motivo opcional de rechazo: <c>POST /api/admin/psicologos/{id}/rechazar</c> (PB-26).</summary>
public class RechazarPsicologoVm
{
    /// <summary>Motivo del rechazo (opcional, va en el correo).</summary>
    public string? Motivo { get; set; }
}

/// <summary>Detalle de solicitud: <c>GET /api/admin/psicologos/{id}/validacion</c> (PB-26).</summary>
public class ValidacionDetalleDto
{
    /// <summary>Id del psicólogo.</summary>
    public int Id { get; set; }

    /// <summary>Nombre completo.</summary>
    public string Nombre { get; set; } = string.Empty;

    /// <summary>Correo.</summary>
    public string Correo { get; set; } = string.Empty;

    /// <summary>Fecha de solicitud (registro).</summary>
    public DateTime FechaSolicitud { get; set; }

    /// <summary>Especialidades registradas (nombres).</summary>
    public List<string> Especialidades { get; set; } = [];

    /// <summary>True si adjuntó tarjeta profesional.</summary>
    public bool TieneTarjeta { get; set; }

    /// <summary>Estado de aprobación actual.</summary>
    public string EstadoAprobacion { get; set; } = string.Empty;

    /// <summary>True si aún está pendiente (se puede aprobar/rechazar).</summary>
    public bool Pendiente { get; set; }

    /// <summary>Aviso cuando ya fue procesada.</summary>
    public string Aviso { get; set; } = string.Empty;
}
