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

/// <summary>Paciente autorizado para asignarle un cuestionario (pantalla "Asignar cuestionario", PB-22).</summary>
public class PacienteAsignarDto
{
    public int Id { get; set; }
    public string Nombre { get; set; } = "";

    /// <summary>Si ya tiene este cuestionario asignado pendiente (el front lo deshabilita).</summary>
    public bool YaAsignado { get; set; }
}

/// <summary>Pantalla "Asignar cuestionario": datos del cuestionario a asignar + pacientes autorizados (PB-22).</summary>
public class AsignarCuestionarioPantallaDto
{
    public int Id { get; set; }
    public string Titulo { get; set; } = "";
    public string Estado { get; set; } = "";
    public List<PacienteAsignarDto> Pacientes { get; set; } = [];
}

/// <summary>Cuerpo de <c>POST /api/psicologo/cuestionarios/{id}/asignar</c> (PB-22).</summary>
public class AsignarCuestionarioRequest
{
    /// <summary>Ids de los pacientes autorizados a asignar el cuestionario (al menos uno).</summary>
    public List<int> PacientesIds { get; set; } = [];
}

/// <summary>Respuesta de <c>POST /api/psicologo/cuestionarios/{id}/asignar</c> (PB-22).</summary>
public class AsignarCuestionarioResult
{
    /// <summary>Mensaje de confirmación exacto del HU.</summary>
    public string Mensaje { get; set; } = "";

    /// <summary>Cantidad de asignaciones registradas (paciente-cuestionario).</summary>
    public int Asignados { get; set; }
}