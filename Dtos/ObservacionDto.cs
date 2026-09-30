namespace Mindzyxs_API.Dtos;

/// <summary>Crear observación: <c>POST /api/observaciones</c> (PB-19 HU1-PB19).</summary>
public class ObservacionCrearDto
{
    /// <summary>Id del paciente autorizado.</summary>
    public int IdPaciente { get; set; }

    /// <summary>Registro al que se asocia (opcional; el front lo preselecciona si viene de un registro).</summary>
    public int? IdEvento { get; set; }

    /// <summary>Texto de la observación (obligatorio).</summary>
    public string Contenido { get; set; } = string.Empty;
}
