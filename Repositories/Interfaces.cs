using Mindzyxs_API.Dtos;

namespace Mindzyxs_API.Repositories;

public interface IUserRepository
{
    Task<UsuarioDto?> FindByCorreoAsync(string correo, CancellationToken ct = default);
    Task<bool> ExistsByCorreoAsync(string correo, CancellationToken ct = default);
    Task<int> AddAsync(UsuarioDto user, CancellationToken ct = default);
    Task SetFilePathAsync(int idUsuario, string path, CancellationToken ct = default);
    Task SetEstadoCuentaAsync(int idUsuario, int idEstado, CancellationToken ct = default);
}

public interface ICatalogRepository
{
    Task<List<CatalogItemDto>> GetTiposCuentaAsync(CancellationToken ct = default);
    Task<List<CatalogItemDto>> GetGenerosAsync(CancellationToken ct = default);
    Task<int> GetRolIdAsync(string nombre, CancellationToken ct = default);
    Task<int> GetEstadoIdAsync(string nombre, CancellationToken ct = default);
    Task<bool> ExisteGeneroAsync(int idGenero, CancellationToken ct = default);
}

public interface ICodigoRepository
{
    Task CrearAsync(int idUsuario, string codigo, string tipo, DateTime expiraEn, CancellationToken ct = default);
    Task< mindzyxsCode?> UltimoAsync(int idUsuario, string tipo, CancellationToken ct = default);
    Task ConsumirAsync(int idCodigo, CancellationToken ct = default);
}

public record mindzyxsCode(int IdCodigo, string Codigo, DateTime ExpiraEn);
