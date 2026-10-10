namespace Mindzyxs_API.Dtos;

/// <summary>Fila de la lista de líneas de ayuda (admin): <c>GET /api/admin/lineas-ayuda</c> (PB-29 HU1-PB29).</summary>
public class LineaAyudaAdminListaDto
{
    public int Id { get; set; }
    public string Nombre { get; set; } = string.Empty;
    public string? Telefono { get; set; }
    public string? Url { get; set; }
    public string Estado { get; set; } = string.Empty;
}

/// <summary>Detalle de una línea de ayuda para editar (admin): <c>GET /api/admin/lineas-ayuda/{id}</c> (PB-29).</summary>
public class LineaAyudaAdminDetalleDto
{
    public int Id { get; set; }
    public string Nombre { get; set; } = string.Empty;
    public string? Descripcion { get; set; }
    public string? Telefono { get; set; }
    public string? Url { get; set; }
    public string? Horario { get; set; }
    public string? Cobertura { get; set; }
    public string? DescripcionCorta { get; set; }
    public string? Whatsapp { get; set; }
    public string? Email { get; set; }
    public string? ChatUrl { get; set; }
    public string? Videollamada { get; set; }
    public string? TipoAtencion { get; set; }
    public string? Departamento { get; set; }
    public string? Municipio { get; set; }
    public string? Poblacion { get; set; }
    public string? Fuente { get; set; }
    public DateOnly? FechaVerificacion { get; set; }
    public string Estado { get; set; } = string.Empty;
}

/// <summary>Cuerpo para crear/editar línea de ayuda (admin): <c>POST/PUT /api/admin/lineas-ayuda</c> (PB-29).</summary>
public class LineaAyudaAdminCrearDto
{
    public string Nombre { get; set; } = string.Empty;
    public string? Descripcion { get; set; }
    public string? Telefono { get; set; }
    public string? Url { get; set; }
    public string? Horario { get; set; }
    public string? Cobertura { get; set; }
    public string? DescripcionCorta { get; set; }
    public string? Whatsapp { get; set; }
    public string? Email { get; set; }
    public string? ChatUrl { get; set; }
    public string? Videollamada { get; set; }
    public string? TipoAtencion { get; set; }
    public string? Departamento { get; set; }
    public string? Municipio { get; set; }
    public string? Poblacion { get; set; }
    public string? Fuente { get; set; }
    public DateOnly? FechaVerificacion { get; set; }
}

/// <summary>Respuesta de crear/editar línea de ayuda (admin).</summary>
public class LineaAyudaAdminResultadoDto
{
    public int Id { get; set; }
    public string Mensaje { get; set; } = string.Empty;
}