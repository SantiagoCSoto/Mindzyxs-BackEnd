namespace Mindzyxs_API.Dtos;

/// <summary>Elemento de catálogo (tipo de cuenta o género).</summary>
public class CatalogItemDto
{
    /// <summary>Id del registro de catálogo.</summary>
    public int Id { get; set; }

    /// <summary>Código corto del catálogo (ej. `Paciente`, `Psicologo`, `Masculino`).</summary>
    public string Codigo { get; set; } = string.Empty;

    /// <summary>Nombre legible del catálogo.</summary>
    public string Nombre { get; set; } = string.Empty;
}

/// <summary>Catálogos del formulario de registro (<c>GET /api/account/catalogos</c>).</summary>
public class RegistroCatalogosDto
{
    /// <summary>Tipos de cuenta disponibles (`Paciente`/`Psicologo`).</summary>
    public List<CatalogItemDto> TiposCuenta { get; set; } = [];

    /// <summary>Géneros disponibles.</summary>
    public List<CatalogItemDto> Generos { get; set; } = [];

    /// <summary>Especialidades disponibles (checklist del registro del psicólogo).</summary>
    public List<CatalogItemDto> Especialidades { get; set; } = [];
}