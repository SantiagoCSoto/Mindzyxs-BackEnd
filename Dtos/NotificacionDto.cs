namespace Mindzyxs_API.Dtos;

/// <summary>Evento del catálogo: <c>GET /api/admin/notificaciones</c> (PB-30).</summary>
public class NotificacionDto
{
    /// <summary>Código del evento (CODE_SENT, PASSWORD_RECOVERY, ...).</summary>
    public string Tipo { get; set; } = string.Empty;

    /// <summary>Nombre legible del evento.</summary>
    public string Nombre { get; set; } = string.Empty;

    /// <summary>Canales (solo correo).</summary>
    public List<string> Canales { get; set; } = [];

    /// <summary>Si está habilitado.</summary>
    public bool Activo { get; set; }

    /// <summary>Asunto (correo).</summary>
    public string? Asunto { get; set; }

    /// <summary>Plantilla con variables {nombre}, {codigo}, {fecha}, {motivo}.</summary>
    public string? Cuerpo { get; set; }
}

/// <summary>Actualizar evento: <c>PUT /api/admin/notificaciones/{tipo}</c> (PB-30).</summary>
public class ActualizarNotificacionVm
{
    /// <summary>Canales (mínimo uno si está activo).</summary>
    public List<string> Canales { get; set; } = [];

    /// <summary>Habilitado.</summary>
    public bool Activo { get; set; }

    /// <summary>Asunto (obligatorio si correo está activo).</summary>
    public string? Asunto { get; set; }

    /// <summary>Plantilla (obligatoria si está activo).</summary>
    public string? Cuerpo { get; set; }
}

/// <summary>Preview: <c>POST /api/admin/notificaciones/{tipo}/preview</c> (PB-30).</summary>
public class PreviewNotificacionVm
{
    /// <summary>Nombre de ejemplo.</summary>
    public string? Nombre { get; set; }

    /// <summary>Código de ejemplo.</summary>
    public string? Codigo { get; set; }

    /// <summary>Fecha de ejemplo.</summary>
    public string? Fecha { get; set; }

    /// <summary>Motivo de ejemplo.</summary>
    public string? Motivo { get; set; }
}
