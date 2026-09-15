using Mindzyxs_API.Dtos;

namespace Mindzyxs_API.Repositories;

public interface IUserRepository
{
    Task<UsuarioDto?> FindByCorreoAsync(string correo, CancellationToken ct = default);
    Task<UsuarioDto?> FindByIdAsync(int idUsuario, CancellationToken ct = default);
    Task<bool> ExistsByCorreoAsync(string correo, CancellationToken ct = default);
    Task<int> AddAsync(UsuarioDto user, CancellationToken ct = default);
    Task SetFilePathAsync(int idUsuario, string path, CancellationToken ct = default);
    Task SetEstadoCuentaAsync(int idUsuario, int idEstado, CancellationToken ct = default);
    Task SetPasswordAsync(int idUsuario, string passwordHash, CancellationToken ct = default);
    Task UpdateUltimoAccesoAsync(int idUsuario, CancellationToken ct = default);
    Task<string?> GetRolNombreAsync(int idRol, CancellationToken ct = default);
    Task<string?> GetEstadoCuentaNombreAsync(int idEstadoCuenta, CancellationToken ct = default);
}

public interface ICatalogRepository
{
    Task<List<CatalogItemDto>> GetTiposCuentaAsync(CancellationToken ct = default);
    Task<List<CatalogItemDto>> GetGenerosAsync(CancellationToken ct = default);
    Task<int> GetRolIdAsync(string nombre, CancellationToken ct = default);
    Task<int> GetEstadoIdAsync(string nombre, CancellationToken ct = default);
    Task<bool> ExisteGeneroAsync(int idGenero, CancellationToken ct = default);
    Task<string?> GetRolNombreAsync(int idRol, CancellationToken ct = default);
    Task<string?> GetEstadoNombreAsync(int idEstado, CancellationToken ct = default);
}

public interface ICodigoRepository
{
    Task CrearAsync(int idUsuario, string codigo, string tipo, DateTime expiraEn, CancellationToken ct = default);
    Task< mindzyxsCode?> UltimoAsync(int idUsuario, string tipo, CancellationToken ct = default);
    Task ConsumirAsync(int idCodigo, CancellationToken ct = default);
}

public interface ISesionRepository
{
    Task<int> CrearAsync(int idUsuario, string tokenHash, DateTime expiraEn, CancellationToken ct = default);
    Task<SesionDto?> FindByTokenHashAsync(string tokenHash, CancellationToken ct = default);
    Task DeleteAsync(int idSesion, CancellationToken ct = default);
    Task DeleteByTokenHashAsync(string tokenHash, CancellationToken ct = default);
}

public interface IAuthLogRepository
{
    Task LogAsync(int? actorId, int? targetId, string accion, string detalle, string? ip, CancellationToken ct = default);
}

public interface IAdminRepository
{
    Task<List<RespaldoDto>> ListRespaldosAsync(CancellationToken ct = default);
    Task CrearRespaldoAsync(string nombreArchivo, string tamano, int idAdmin, CancellationToken ct = default);
    Task<int> LimpiarCodigosExpiradosAsync(CancellationToken ct = default);
    Task<int> LimpiarSesionesExpiradasAsync(CancellationToken ct = default);
    Task<int> LimpiarAuthAntiguosAsync(int dias, CancellationToken ct = default);
    Task<string> ExportSnapshotJsonAsync(CancellationToken ct = default);
}

public record RespaldoDto(int IdRespaldo, string NombreArchivo, string? Tamano, DateTime FechaHora, int IdAdmin);
public record mindzyxsCode(int IdCodigo, string Codigo, DateTime ExpiraEn);
public record SesionDto(int IdSesion, int IdUsuario, string TokenHash, DateTime ExpiraEn, DateTime CreadaEn);
