namespace Mindzyxs_API.Dtos;

/// <summary>Fila de la lista de cuestionarios predeterminados: <c>GET /api/admin/cuestionarios-predeterminados</c> (PB-28 HU1-PB28).</summary>
public class CuestionarioPredeterminadoListaDto
{
    public int Id { get; set; }
    public string Titulo { get; set; } = string.Empty;
    public string? Descripcion { get; set; }
    public string Estado { get; set; } = string.Empty;
    public int NumPreguntas { get; set; }
    public DateTime FechaCreacion { get; set; }
}

/// <summary>Detalle editable de un cuestionario predeterminado: <c>GET /api/admin/cuestionarios-predeterminados/{id}</c> (PB-28).</summary>
public class CuestionarioPredeterminadoDetalleDto
{
    public int Id { get; set; }
    public string Titulo { get; set; } = string.Empty;
    public string? Descripcion { get; set; }
    public string Estado { get; set; } = string.Empty;
    public string? ModoCalculo { get; set; }
    public int? UmbralBajoMax { get; set; }
    public int? UmbralMedioMax { get; set; }
    public DateTime FechaCreacion { get; set; }
    public List<PreguntaPsicologoDto> Preguntas { get; set; } = [];
}

/// <summary>Cuerpo para crear/editar cuestionario predeterminado: <c>POST/PUT /api/admin/cuestionarios-predeterminados</c> (PB-28).</summary>
public class CuestionarioPredeterminadoCrearDto
{
    public string Titulo { get; set; } = string.Empty;
    public string? Descripcion { get; set; }
    public bool Publicar { get; set; }
    public UmbralesRiesgoDto? Umbrales { get; set; }
    public List<PreguntaCrearDto> Preguntas { get; set; } = [];
}

/// <summary>Respuesta de crear/editar cuestionario predeterminado.</summary>
public class CuestionarioPredeterminadoResultadoDto
{
    public int Id { get; set; }
    public string Mensaje { get; set; } = string.Empty;
}