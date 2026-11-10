namespace Mindzyxs_API.Dtos;

/// <summary>Psicólogo con estado de autorización: <c>GET /api/accesos</c> (PB-17 HU1-PB17).</summary>
public class AccesoPsicologoDto
{
    /// <summary>Id del psicólogo.</summary>
    public int IdPsicologo { get; set; }

    /// <summary>Nombre completo.</summary>
    public string Nombre { get; set; } = string.Empty;

    /// <summary>Especialidades (vacía si no registró ninguna).</summary>
    public List<string> Especialidades { get; set; } = [];

    /// <summary>True si está Autorizado, false si No autorizado.</summary>
    public bool Autorizado { get; set; }
}
