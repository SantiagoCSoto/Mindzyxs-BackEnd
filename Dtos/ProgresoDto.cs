namespace Mindzyxs_API.Dtos;

/// <summary>Paciente autorizado para el psicólogo: <c>GET /api/progreso/pacientes</c>.</summary>
public class ProgresoPacienteDto
{
    /// <summary>Id del paciente.</summary>
    public int Id { get; set; }

    /// <summary>Nombre completo.</summary>
    public string Nombre { get; set; } = "";
}

/// <summary>Punto de la serie temporal para el gráfico.</summary>
public class ProgresoPuntoDto
{
    /// <summary>Fecha del registro.</summary>
    public DateTime Fecha { get; set; }

    /// <summary>Puntaje 1-5 (si aplica).</summary>
    public int? Puntaje { get; set; }

    /// <summary>Nivel (si aplica).</summary>
    public string? Nivel { get; set; }
}

/// <summary>Agregado semanal.</summary>
public class ProgresoSemanaDto
{
    /// <summary>Lunes de la semana (aaaa-MM-dd).</summary>
    public string Semana { get; set; } = "";

    /// <summary>Cantidad de registros.</summary>
    public int Registros { get; set; }

    /// <summary>Promedio de puntaje (si aplica).</summary>
    public double? Promedio { get; set; }
}

/// <summary>Progreso de un paciente: <c>GET /api/progreso/{idPaciente}</c> (PB-14 HU1-PB14 / PB-23 HU1-PB23).</summary>
public class ProgresoDto
{
    /// <summary>Promedio de estado emocional (si aplica).</summary>
    public double? Promedio { get; set; }

    /// <summary>Tendencia: Sube, Baja o Estable (2da mitad vs 1ra, ±0.5).</summary>
    public string Tendencia { get; set; } = "Estable";

    /// <summary>Cantidad de registros en el rango.</summary>
    public int TotalRegistros { get; set; }

    /// <summary>Distribución por nivel (Bajo/Medio/Alto).</summary>
    public Dictionary<string, int> Distribucion { get; set; } = new();

    /// <summary>Agregados por semana.</summary>
    public List<ProgresoSemanaDto> PorSemana { get; set; } = [];

    /// <summary>Agregados por mes.</summary>
    public List<ProgresoMesDto> PorMes { get; set; } = [];

    /// <summary>Serie temporal para el gráfico.</summary>
    public List<ProgresoPuntoDto> Serie { get; set; } = [];

    /// <summary>Nivel más frecuente en el rango (Bajo/Medio/Alto).</summary>
    public string? NivelFrecuente { get; set; }

    /// <summary>False con pocos datos: el front muestra "El paciente no tiene datos suficientes para generar estadísticas".</summary>
    public bool Suficientes { get; set; }
}

/// <summary>Agregado mensual.</summary>
public class ProgresoMesDto
{
    /// <summary>Mes (aaaa-MM).</summary>
    public string Mes { get; set; } = "";

    /// <summary>Cantidad de registros.</summary>
    public int Registros { get; set; }

    /// <summary>Promedio de puntaje (si aplica).</summary>
    public double? Promedio { get; set; }
}
