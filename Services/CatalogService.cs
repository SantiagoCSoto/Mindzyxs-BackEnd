using Mindzyxs_API.Dtos;
using Mindzyxs_API.Repositories;
using Mindzyxs_API.Utilities;

namespace Mindzyxs_API.Services;

public class CatalogService(ICatalogRepository catalogs)
{
    public async Task<RegistroCatalogosDto> GetRegistroAsync(CancellationToken ct = default) => new()
    {
        TiposCuenta = await catalogs.GetTiposCuentaAsync(ct),
        Generos = await catalogs.GetGenerosAsync(ct)
    };
}
