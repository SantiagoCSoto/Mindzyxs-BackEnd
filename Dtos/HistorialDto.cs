namespace Mindzyxs_API.Dtos;

/// <summary>Ítem del historial emocional: <c>GET /api/historial</c> (PB-13 HU1-PB13).</summary>
public class HistorialItemDto
{
    /// <summary>Id del evento (registro o cuestionario).</summary>
    public int Id { get; set; }

    /// <summary>Fecha y hora del registro.</summary>
    public DateTime Fecha { get; set; }

    /// <summary>Tipo: RegistroEmocional o Cuestionario.</summary>
    public string Tipo { get; set; } = "";

    /// <summary>Puntaje 1-5 (si aplica).</summary>
    public int? Puntaje { get; set; }

    /// <summary>Nivel de riesgo (si aplica).</summary>
    public string? Nivel { get; set; }

    /// <summary>Título del cuestionario (si aplica).</summary>
    public string? Cuestionario { get; set; }

    /// <summary>Resumen corto para la lista.</summary>
    public string Resumen { get; set; } = "";
}

/// <summary>Respuesta con texto de pregunta (detalle del historial).</summary>
public class HistorialRespuestaDto
{
    /// <summary>Texto de la pregunta.</summary>
    public string Pregunta { get; set; } = "";

    /// <summary>Valor numérico.</summary>
    public int? Valor { get; set; }

    /// <summary>Texto libre (si aplica).</summary>
    public string? Texto { get; set; }
}

/// <summary>Apoyo del psicólogo sobre el evento (detalle del historial).</summary>
public class HistorialObservacionDto
{
    /// <summary>Contenido del mensaje de apoyo.</summary>
    public string Contenido { get; set; } = "";

    /// <summary>Firma del psicólogo.</summary>
    public string? Firma { get; set; }

    /// <summary>Fecha del mensaje.</summary>
    public DateTime Fecha { get; set; }
}

/// <summary>Detalle de un registro: <c>GET /api/historial/{id}</c>.</summary>
public class HistorialDetalleDto : HistorialItemDto
{
    /// <summary>Cómo me sentí (nota propia del paciente).</summary>
    public string? Nota { get; set; }

    /// <summary>Respuestas del cuestionario (si aplica).</summary>
    public List<HistorialRespuestaDto> Respuestas { get; set; } = [];

    /// <summary>Apoyo del psicólogo (si hay).</summary>
    public List<HistorialObservacionDto> Observaciones { get; set; } = [];
}

/// <summary>Registro del estado emocional: <c>POST /api/historial</c> (PB-12 HU1-PB12).</summary>
/// <remarks>
/// El paciente selecciona su estado en la escala 1-5 (1 Muy mal / 2 Mal / 3 Regular /
/// 4 Bien / 5 Excelente, según el mockup del PB) y puede escribir una nota opcional.
/// La fecha y hora se asignan automáticamente al guardar. Sin escala → 400 con
/// "Debes seleccionar un estado emocional para guardar".
/// </remarks>
public class RegistrarEstadoEmocionalVm
{
    /// <summary>Estado en la escala 1-5 (obligatorio).</summary>
    public int? Puntaje { get; set; }

    /// <summary>Nota breve opcional (cómo me sentí).</summary>
    public string? Nota { get; set; }
}

/// <summary>Resultado de guardar el estado emocional (el controller agrega el mensaje).</summary>
public record RegistroEmocionalCreadoResult(int IdEvento, DateTime Fecha, int Puntaje, string Nivel, string? Nota);
