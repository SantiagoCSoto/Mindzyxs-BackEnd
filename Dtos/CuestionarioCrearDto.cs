namespace Mindzyxs_API.Dtos;

/// <summary>Crear cuestionario personalizado: <c>POST /api/psicologo/cuestionarios</c> (PB-20 HU1-PB20).</summary>
public class CuestionarioCrearDto
{
    /// <summary>Título del cuestionario (obligatorio).</summary>
    public string Titulo { get; set; } = string.Empty;

    /// <summary>Descripción o instrucciones (opcional).</summary>
    public string? Descripcion { get; set; }

    /// <summary>True = Publicar de una vez; false = Guardar como Borrador.</summary>
    public bool Publicar { get; set; }

    /// <summary>Intervalos Alto/Medio/Bajo sobre la suma (opcional; sin esto rige la regla actual).</summary>
    public UmbralesRiesgoDto? Umbrales { get; set; }

    /// <summary>Preguntas con sus opciones (mínimo 1 pregunta).</summary>
    public List<PreguntaCrearDto> Preguntas { get; set; } = [];
}

/// <summary>Umbrales de riesgo sobre la suma de valores (PB-20).</summary>
public class UmbralesRiesgoDto
{
    /// <summary>Puntaje máximo considerado Bajo.</summary>
    public int BajoMax { get; set; }

    /// <summary>Puntaje máximo considerado Medio (más = Alto).</summary>
    public int MedioMax { get; set; }
}

/// <summary>Pregunta con sus opciones (PB-20 crear; PB-21 editar si trae <see cref="Id"/>).</summary>
public class PreguntaCrearDto
{
    /// <summary>Id al EDITAR (PB-21): nulo en preguntas nuevas, presente en las que ya existen.</summary>
    public int? Id { get; set; }

    /// <summary>Texto de la pregunta (obligatorio).</summary>
    public string Texto { get; set; } = string.Empty;

    /// <summary>Tipo ('escala' u 'opcion_multiple' entran al puntaje; por defecto 'opcion_multiple').</summary>
    public string? Tipo { get; set; }

    /// <summary>Opciones con valor asignado (mínimo 2).</summary>
    public List<OpcionCrearDto> Opciones { get; set; } = [];
}

/// <summary>Opción de respuesta con valor numérico (PB-20).</summary>
public class OpcionCrearDto
{
    /// <summary>Texto de la opción (obligatorio).</summary>
    public string Texto { get; set; } = string.Empty;

    /// <summary>Valor numérico para el puntaje (>= 0).</summary>
    public int Valor { get; set; }
}

/// <summary>Resultado de crear el cuestionario (PB-20).</summary>
public class CuestionarioCreadoDto
{
    public int Id { get; set; }
    public string Mensaje { get; set; } = string.Empty;
}
