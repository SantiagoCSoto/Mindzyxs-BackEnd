namespace Mindzyxs_API.Dtos;

public class CatalogItemDto
{
    public int Id { get; set; }
    public string Codigo { get; set; } = string.Empty;
    public string Nombre { get; set; } = string.Empty;
}

public class RegistroCatalogosDto
{
    public List<CatalogItemDto> TiposCuenta { get; set; } = [];
    public List<CatalogItemDto> Generos { get; set; } = [];
}
