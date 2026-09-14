using Microsoft.EntityFrameworkCore;
using Mindzyxs_API.Dtos;
using Mindzyxs_API.Repositories.Models;
using Mindzyxs_API.Utilities;

namespace Mindzyxs_API.Repositories;

public class EfUserRepository(MindzyxsDbContext db) : IUserRepository
{
    public async Task<UsuarioDto?> FindByCorreoAsync(string correo, CancellationToken ct = default)
    {
        var c = ContactHelper.Normalize(correo).ToLower();
        var u = await db.Usuarios.AsNoTracking().FirstOrDefaultAsync(x => x.Correo.ToLower() == c, ct);
        return u is null ? null : Map(u);
    }

    public async Task<bool> ExistsByCorreoAsync(string correo, CancellationToken ct = default)
    {
        var c = ContactHelper.Normalize(correo).ToLower();
        return await db.Usuarios.AnyAsync(x => x.Correo.ToLower() == c, ct);
    }

    public async Task<int> AddAsync(UsuarioDto user, CancellationToken ct = default)
    {
        var row = new UsuarioRow
        {
            Nombre = user.Nombre.Trim(),
            Apellido = user.Apellido.Trim(),
            Correo = ContactHelper.Normalize(user.Correo),
            PasswordHash = user.PasswordHash,
            FechaNacimiento = user.FechaNacimiento,
            IdRol = user.IdRol,
            IdEstadoCuenta = user.IdEstadoCuenta,
            IdEstadoAprobacion = user.IdEstadoAprobacion,
            IdGenero = user.IdGenero,
            FechaRegistro = DateTime.UtcNow
        };
        db.Usuarios.Add(row);
        await db.SaveChangesAsync(ct);
        return row.IdUsuario;
    }

    public async Task SetFilePathAsync(int idUsuario, string path, CancellationToken ct = default)
        => await db.Usuarios.Where(x => x.IdUsuario == idUsuario)
            .ExecuteUpdateAsync(s => s.SetProperty(x => x.FilePath, path), ct);

    public async Task SetEstadoCuentaAsync(int idUsuario, int idEstado, CancellationToken ct = default)
        => await db.Usuarios.Where(x => x.IdUsuario == idUsuario)
            .ExecuteUpdateAsync(s => s.SetProperty(x => x.IdEstadoCuenta, idEstado), ct);

    private static UsuarioDto Map(UsuarioRow u) => new()
    {
        IdUsuario = u.IdUsuario,
        IdRol = u.IdRol,
        IdEstadoCuenta = u.IdEstadoCuenta,
        IdEstadoAprobacion = u.IdEstadoAprobacion,
        Nombre = u.Nombre,
        Apellido = u.Apellido,
        Correo = u.Correo,
        PasswordHash = u.PasswordHash,
        FechaNacimiento = u.FechaNacimiento,
        IdGenero = u.IdGenero,
        FilePath = u.FilePath
    };
}

public class EfCatalogRepository(MindzyxsDbContext db) : ICatalogRepository
{
    public async Task<List<CatalogItemDto>> GetTiposCuentaAsync(CancellationToken ct = default)
        => await db.Roles.AsNoTracking()
            .Where(r => r.Nombre == AppRoles.Paciente || r.Nombre == AppRoles.Psicologo)
            .OrderBy(r => r.Nombre)
            .Select(r => new CatalogItemDto { Id = r.IdRol, Codigo = r.Nombre, Nombre = r.Nombre })
            .ToListAsync(ct);

    public async Task<List<CatalogItemDto>> GetGenerosAsync(CancellationToken ct = default)
        => await db.Generos.AsNoTracking().OrderBy(g => g.IdGenero)
            .Select(g => new CatalogItemDto { Id = g.IdGenero, Codigo = g.Nombre, Nombre = g.Nombre })
            .ToListAsync(ct);

    public async Task<int> GetRolIdAsync(string nombre, CancellationToken ct = default)
        => await db.Roles.Where(r => r.Nombre == nombre).Select(r => r.IdRol).FirstAsync(ct);

    public async Task<int> GetEstadoIdAsync(string nombre, CancellationToken ct = default)
        => await db.Estados.Where(e => e.Nombre == nombre).Select(e => e.IdEstado).FirstAsync(ct);

    public async Task<bool> ExisteGeneroAsync(int idGenero, CancellationToken ct = default)
        => await db.Generos.AnyAsync(g => g.IdGenero == idGenero, ct);
}

public class EfCodigoRepository(MindzyxsDbContext db) : ICodigoRepository
{
    public async Task CrearAsync(int idUsuario, string codigo, string tipo, DateTime expiraEn, CancellationToken ct = default)
    {
        db.Codigos.Add(new CodigoRow
        {
            IdUsuario = idUsuario,
            Codigo = codigo,
            Tipo = tipo,
            ExpiraEn = expiraEn,
            CreadoEn = DateTime.UtcNow
        });
        await db.SaveChangesAsync(ct);
    }

    public async Task<mindzyxsCode?> UltimoAsync(int idUsuario, string tipo, CancellationToken ct = default)
        => await db.Codigos.AsNoTracking()
            .Where(c => c.IdUsuario == idUsuario && c.Tipo == tipo)
            .OrderByDescending(c => c.IdCodigo)
            .Select(c => new mindzyxsCode(c.IdCodigo, c.Codigo, c.ExpiraEn))
            .FirstOrDefaultAsync(ct);

    public async Task ConsumirAsync(int idCodigo, CancellationToken ct = default)
        => await db.Codigos.Where(c => c.IdCodigo == idCodigo).ExecuteDeleteAsync(ct);
}
