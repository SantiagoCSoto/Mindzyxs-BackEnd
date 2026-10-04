namespace Mindzyxs_API.Dtos;

/// <summary>Fila de la lista "Mis cuestionarios": <c>GET /api/psicologo/cuestionarios</c> (PB-21 HU1-PB21).</summary>
public record CuestionarioPsicologoListaDto(
    int Id,
    string Titulo,
    string? Descripcion,
    string Estado,
    int NumPreguntas,
    DateTime FechaCreacion);

/// <summary>Detalle del cuestionario del psicólogo: <c>GET /api/psicologo/cuestionarios/{id}</c> (PB-21).</summary>
public class CuestionarioPsicologoDetalleDto
{
    public int Id { get; set; }
    public string Titulo { get; set; } = "";
    public string? Descripcion { get; set; }
    public string Estado { get; set; } = "";
    public string? ModoCalculo { get; set; }
    public int? UmbralBajoMax { get; set; }
    public int? UmbralMedioMax { get; set; }
    public DateTime FechaCreacion { get; set; }
    public List<PreguntaPsicologoDto> Preguntas { get; set; } = [];
}

/// <summary>Pregunta del detalle editable (con su id para mapear el guardado).</summary>
public class PreguntaPsicologoDto
{
    public int Id { get; set; }
    public string Texto { get; set; } = "";
    public int Orden { get; set; }
    public string? Tipo { get; set; }
    public List<OpcionPsicologoDto> Opciones { get; set; } = [];
}

/// <summary>Opción del detalle editable (con su id).</summary>
public class OpcionPsicologoDto
{
    public int Id { get; set; }
    public string Texto { get; set; } = "";
    public int Valor { get; set; }
    public int Orden { get; set; }
}