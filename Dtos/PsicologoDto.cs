namespace Mindzyxs_API.Dtos;

/// <summary>Directorio de psicólogos para pacientes: <c>GET /api/psicologos</c>.</summary>
public class PsicologoDirectorioDto
{
    /// <summary>Id del psicólogo.</summary>
    public int Id { get; set; }

    /// <summary>Nombre completo.</summary>
    public string Nombre { get; set; } = string.Empty;

    /// <summary>Especialidades (vacía si no registró ninguna).</summary>
    public List<string> Especialidades { get; set; } = [];

    /// <summary>Pacientes asignados (autorizaciones en Autorizado).</summary>
    public int PacientesAsignados { get; set; }
}
