namespace Mindzyxs_API.Dtos;

/// <summary>
/// Contenido del Mensaje aclaratorio servido por <c>GET /api/aviso</c> (PB-04 HU1-PB4).
/// </summary>
public class AvisoDto
{
    /// <summary>Título de la pantalla/modal: "Mensaje aclaratorio" o "Aviso importante".</summary>
    public string Titulo { get; set; } = "";

    /// <summary>Texto completo del mensaje, con párrafos separados por doble salto de línea.</summary>
    public string Texto { get; set; } = "";
}

/// <summary>Requets de registro de visualización: <c>POST /api/aviso/visualizacion</c>.</summary>
public class VisualizacionAvisoVm
{
    /// <summary>Origen de la visualización: <c>enlace</c> (barra inferior) o <c>auto:&lt;funcion&gt;</c> (antes de funcionalidad sensible).</summary>
    public string? Origen { get; set; }

    /// <summary>Resultado de la carga del mensaje: <c>ok</c> o <c>error</c>.</summary>
    public string? Resultado { get; set; }
}