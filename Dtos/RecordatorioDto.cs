namespace Mindzyxs_API.Dtos;

/// <summary>Configuración de recordatorios: <c>GET /api/recordatorios</c> (PB-15 HU1-PB15).</summary>
public class RecordatorioDto
{
    /// <summary>Frecuencia: Diaria o Semanal.</summary>
    public string Frecuencia { get; set; } = "Diaria";

    /// <summary>Hora programada (HH:mm).</summary>
    public string Hora { get; set; } = "";

    /// <summary>Días 1=Lunes..7=Domingo (solo Semanal; Diaria = todos implícitos).</summary>
    public List<int> Dias { get; set; } = [];

    /// <summary>Título del aviso.</summary>
    public string? Titulo { get; set; }

    /// <summary>Mensaje del aviso.</summary>
    public string? Mensaje { get; set; }

    /// <summary>Si los avisos están activos.</summary>
    public bool Activo { get; set; } = true;
}

/// <summary>Guardar recordatorios: <c>POST /api/recordatorios</c>.</summary>
public class GuardarRecordatorioVm
{
    /// <summary>Frecuencia: Diaria o Semanal (obligatoria).</summary>
    public string? Frecuencia { get; set; }

    /// <summary>Hora programada HH:mm (obligatoria).</summary>
    public string? Hora { get; set; }

    /// <summary>Días 1-7 (requeridos si Semanal).</summary>
    public List<int> Dias { get; set; } = [];

    /// <summary>Título del aviso.</summary>
    public string? Titulo { get; set; }

    /// <summary>Mensaje del aviso.</summary>
    public string? Mensaje { get; set; }

    /// <summary>Activar o desactivar (default true).</summary>
    public bool? Activo { get; set; }
}

/// <summary>Aviso vencido de hoy: <c>GET /api/recordatorios/hoy</c> (para polling del front).</summary>
public class RecordatorioHoyDto
{
    /// <summary>Título del aviso.</summary>
    public string Titulo { get; set; } = "";

    /// <summary>Mensaje del aviso.</summary>
    public string? Mensaje { get; set; }

    /// <summary>Hora programada que ya pasó hoy.</summary>
    public string Hora { get; set; } = "";
}
