using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using Mindzyxs_API.Dtos;
using Mindzyxs_API.Repositories.Models;
using Mindzyxs_API.Services;
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

    public async Task<UsuarioDto?> FindByIdAsync(int idUsuario, CancellationToken ct = default)
    {
        var u = await db.Usuarios.AsNoTracking().FirstOrDefaultAsync(x => x.IdUsuario == idUsuario, ct);
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

    // PB-05 HU2-PB5: actualiza password_hash al restablecer la contraseña.
    public async Task SetPasswordAsync(int idUsuario, string passwordHash, CancellationToken ct = default)
        => await db.Usuarios.Where(x => x.IdUsuario == idUsuario)
            .ExecuteUpdateAsync(s => s.SetProperty(x => x.PasswordHash, passwordHash), ct);

    public async Task UpdateUltimoAccesoAsync(int idUsuario, CancellationToken ct = default)
        => await db.Usuarios.Where(x => x.IdUsuario == idUsuario)
            .ExecuteUpdateAsync(s => s.SetProperty(x => x.UltimoAcceso, DateTime.UtcNow), ct);

    // Intento actualizar ultimo_acceso si existe; si no, no falla.
    public async Task<string?> GetRolNombreAsync(int idRol, CancellationToken ct = default)
        => await db.Roles.Where(r => r.IdRol == idRol).Select(r => r.Nombre).FirstOrDefaultAsync(ct);

    public async Task<string?> GetEstadoCuentaNombreAsync(int idEstadoCuenta, CancellationToken ct = default)
        => await db.Estados.Where(e => e.IdEstado == idEstadoCuenta).Select(e => e.Nombre).FirstOrDefaultAsync(ct);

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

    public async Task<string?> GetRolNombreAsync(int idRol, CancellationToken ct = default)
        => await db.Roles.Where(r => r.IdRol == idRol).Select(r => r.Nombre).FirstOrDefaultAsync(ct);

    public async Task<string?> GetEstadoNombreAsync(int idEstado, CancellationToken ct = default)
        => await db.Estados.Where(e => e.IdEstado == idEstado).Select(e => e.Nombre).FirstOrDefaultAsync(ct);
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

public class EfSesionRepository(MindzyxsDbContext db) : ISesionRepository
{
    // Corrección BD: sp_sesion_crear inserta; TR_sesiones_Limite3 y
    // TR_sesiones_AuditCreacion aplican límite de 3 + audit. Devuelve id_sesion.
    public async Task<int> CrearAsync(int idUsuario, string tokenHash, DateTime expiraEn, CancellationToken ct = default)
    {
        var ids = await db.Database.SqlQueryRaw<int>(
            "EXEC dbo.sp_sesion_crear @u, @h, @e",
            new SqlParameter("@u", idUsuario),
            new SqlParameter("@h", tokenHash),
            new SqlParameter("@e", expiraEn)).ToListAsync(ct);
        return ids.Single();
    }

    public async Task<SesionDto?> FindByTokenHashAsync(string tokenHash, CancellationToken ct = default)
        => await db.Sesiones.AsNoTracking()
            .Where(s => s.TokenHash == tokenHash)
            .Select(s => new SesionDto(s.IdSesion, s.IdUsuario, s.TokenHash, s.ExpiraEn, s.CreadaEn))
            .FirstOrDefaultAsync(ct);

    public async Task DeleteAsync(int idSesion, CancellationToken ct = default)
        => await db.Sesiones.Where(s => s.IdSesion == idSesion).ExecuteDeleteAsync(ct);

    public async Task DeleteByTokenHashAsync(string tokenHash, CancellationToken ct = default)
        => await db.Sesiones.Where(s => s.TokenHash == tokenHash).ExecuteDeleteAsync(ct);
}

public class EfAuthLogRepository(MindzyxsDbContext db) : IAuthLogRepository
{
    // Corrección BD: el INSERT vive en sp_auth_registrar (fecha del servidor).
    // OJO: los parámetros van en array explícito; si `ct` cae en el params
    // object[] EF intenta mapear CancellationToken y falla ("no store type mapping").
    public async Task LogAsync(int? actorId, int? targetId, string accion, string detalle, string? ip, CancellationToken ct = default)
    {
        object[] parameters =
        [
            new SqlParameter("@actor", (object?)actorId ?? DBNull.Value),
            new SqlParameter("@target", (object?)targetId ?? DBNull.Value),
            new SqlParameter("@accion", accion),
            new SqlParameter("@detalle", (object?)detalle ?? DBNull.Value),
            new SqlParameter("@ip", (object?)ip ?? DBNull.Value)
        ];
        await db.Database.ExecuteSqlRawAsync(
            "EXEC dbo.sp_auth_registrar @actor, @target, @accion, @detalle, @ip",
            parameters, ct);
    }
}

// PB-07: cuestionarios Predeterminado + Publicado (invitados y pacientes).
public class EfCuestionarioRepository(MindzyxsDbContext db) : ICuestionarioRepository
{
    public Task<List<CuestionarioRow>> GetPublicadosInvitadoAsync(CancellationToken ct = default)
        => db.Cuestionarios.AsNoTracking()
            .Where(c => c.Tipo == "Predeterminado" && c.Estado == "Publicado")
            .OrderBy(c => c.IdCuestionario)
            .ToListAsync(ct);

    public Task<CuestionarioRow?> FindPublicadoInvitadoAsync(int idCuestionario, CancellationToken ct = default)
        => db.Cuestionarios.AsNoTracking()
            .Where(c => c.IdCuestionario == idCuestionario && c.Tipo == "Predeterminado" && c.Estado == "Publicado")
            .FirstOrDefaultAsync(ct);

    // Corrección BD: sp_cuestionario_completo trae preguntas+opciones en 1 round-trip
    // (3 result sets; el 1ro es el cuestionario, ya validado por Find). Multi-result vía reader.
    public async Task<(List<PreguntaRow> Preguntas, List<OpcionPreguntaRow> Opciones)> GetCompletoAsync(int idCuestionario, CancellationToken ct = default)
    {
        var preguntas = new List<PreguntaRow>();
        var opciones = new List<OpcionPreguntaRow>();
        var conn = db.Database.GetDbConnection();
        var opened = false;
        if (conn.State != System.Data.ConnectionState.Open)
        {
            await db.Database.OpenConnectionAsync(ct);
            opened = true;
        }
        try
        {
            await using var cmd = conn.CreateCommand();
            cmd.CommandText = "EXEC dbo.sp_cuestionario_completo @id";
            var p = cmd.CreateParameter();
            p.ParameterName = "@id";
            p.Value = idCuestionario;
            cmd.Parameters.Add(p);
            await using var reader = await cmd.ExecuteReaderAsync(ct);
            await reader.NextResultAsync(ct); // set 1: cuestionario (se omite)
            while (await reader.ReadAsync(ct))
                preguntas.Add(new PreguntaRow
                {
                    IdPregunta = reader.GetInt32(0),
                    IdCuestionario = reader.GetInt32(1),
                    Texto = reader.GetString(2),
                    Orden = reader.GetInt32(3),
                    Tipo = await reader.IsDBNullAsync(4, ct) ? null : reader.GetString(4),
                    PuntajeMax = await reader.IsDBNullAsync(5, ct) ? null : reader.GetInt32(5)
                });
            await reader.NextResultAsync(ct); // set 3: opciones
            while (await reader.ReadAsync(ct))
                opciones.Add(new OpcionPreguntaRow
                {
                    IdOpcion = reader.GetInt32(0),
                    IdPregunta = reader.GetInt32(1),
                    Texto = reader.GetString(2),
                    Valor = reader.GetInt32(3),
                    Orden = reader.GetInt32(4)
                });
        }
        finally
        {
            if (opened) await conn.CloseAsync();
        }
        return (preguntas, opciones);
    }
}

// PB-08: líneas de ayuda en estado Activa.
public class EfLineaAyudaRepository(MindzyxsDbContext db) : ILineaAyudaRepository
{
    public Task<List<LineaAyudaRow>> GetActivasAsync(CancellationToken ct = default)
        => db.LineasAyuda.AsNoTracking()
            .Where(l => l.Estado == "Activa")
            .OrderBy(l => l.IdLinea)
            .ToListAsync(ct);
}

// PB-09: recomendaciones básicas (el admin las crea/edita/desactiva, PB-28).
public class EfRecomendacionRepository(MindzyxsDbContext db) : IRecomendacionRepository
{
    // Corrección BD: el orden por nivel vive en sp_recomendaciones_priorizadas.
    public Task<List<RecomendacionRow>> GetPriorizadasAsync(string? nivel, CancellationToken ct = default)
        => db.Recomendaciones.FromSqlRaw("EXEC dbo.sp_recomendaciones_priorizadas @nivel",
            new SqlParameter("@nivel", (object?)nivel ?? DBNull.Value))
            .AsNoTracking().ToListAsync(ct);

    public Task<RecomendacionRow?> FindByIdAsync(int idRecomendacion, CancellationToken ct = default)
        => db.Recomendaciones.AsNoTracking()
            .Where(r => r.IdRecomendacion == idRecomendacion)
            .FirstOrDefaultAsync(ct);
}

// PB-31: conversaciones en SQL.
public class EfChatRepository(MindzyxsDbContext db) : IChatRepository
{
    public Task<List<ConversacionRow>> GetDeUsuarioAsync(int idUsuario, CancellationToken ct = default)
        => db.Conversaciones.AsNoTracking()
            .Where(c => c.IdPaciente == idUsuario || c.IdPsicologo == idUsuario)
            .OrderByDescending(c => c.FechaCreacion)
            .ToListAsync(ct);

    public Task<ConversacionRow?> FindAsync(int idConversacion, CancellationToken ct = default)
        => db.Conversaciones.AsNoTracking()
            .Where(c => c.IdConversacion == idConversacion)
            .FirstOrDefaultAsync(ct);

    public Task<ConversacionRow?> FindParejaAsync(int idPaciente, int idPsicologo, CancellationToken ct = default)
        => db.Conversaciones
            .Where(c => c.IdPaciente == idPaciente && c.IdPsicologo == idPsicologo)
            .FirstOrDefaultAsync(ct);

    public async Task<ConversacionRow> CrearAsync(int idPaciente, int idPsicologo, CancellationToken ct = default)
    {
        var row = new ConversacionRow
        {
            IdPaciente = idPaciente,
            IdPsicologo = idPsicologo,
            Estado = "Pendiente",
            FechaCreacion = DateTime.UtcNow
        };
        db.Conversaciones.Add(row);
        await db.SaveChangesAsync(ct);
        return row;
    }

    public async Task SetEstadoAsync(int idConversacion, string estado, int? idBloqueador, CancellationToken ct = default)
    {
        var row = await db.Conversaciones
            .Where(c => c.IdConversacion == idConversacion)
            .FirstOrDefaultAsync(ct);
        if (row is null) return;
        row.Estado = estado;
        row.IdBloqueador = idBloqueador;
        row.FechaRespuesta = estado == "Pendiente" ? null : (DateTime?)DateTime.UtcNow;
        await db.SaveChangesAsync(ct);
    }

    // PB-31: buscador de pacientes (activos) con su último riesgo registrado.
    public async Task<List<PacienteBusquedaDto>> BuscarPacientesAsync(int idRolPaciente, int idEstadoActiva, string? q, CancellationToken ct = default)
    {
        var pacientes = await db.Usuarios.AsNoTracking()
            .Where(u => u.IdRol == idRolPaciente && u.IdEstadoCuenta == idEstadoActiva
                && (string.IsNullOrWhiteSpace(q)
                    || u.Nombre.Contains(q) || u.Apellido.Contains(q) || u.Correo.Contains(q)))
            .OrderBy(u => u.Nombre).ThenBy(u => u.Apellido)
            .Take(20)
            .ToListAsync(ct);
        if (pacientes.Count == 0) return [];
        var ids = pacientes.Select(u => u.IdUsuario).ToList();
        var ultimos = await db.Eventos.AsNoTracking()
            .Where(e => ids.Contains(e.IdPaciente))
            .GroupBy(e => e.IdPaciente)
            .Select(g => g.OrderByDescending(e => e.FechaEvento).First())
            .ToListAsync(ct);
        var niveles = await db.NivelesRiesgo.AsNoTracking().ToListAsync(ct);
        return pacientes.Select(u =>
        {
            var ev = ultimos.FirstOrDefault(e => e.IdPaciente == u.IdUsuario);
            var nivel = ev?.IdNivelRiesgo is null
                ? null
                : niveles.FirstOrDefault(n => n.IdNivelRiesgo == ev.IdNivelRiesgo)?.Nombre;
            return new PacienteBusquedaDto(
                u.IdUsuario, $"{u.Nombre} {u.Apellido}".Trim(), u.Correo,
                ev?.PuntajeTotal, nivel, ev?.FechaEvento);
        }).ToList();
    }
}

public class EfAdminRepository(MindzyxsDbContext db, IOptions<AdminOptions> adminOptions) : IAdminRepository
{
    public async Task<List<RespaldoDto>> ListRespaldosAsync(CancellationToken ct = default)
        => await db.Respaldos.AsNoTracking()
            .OrderByDescending(r => r.FechaHora)
            .Select(r => new RespaldoDto(r.IdRespaldo, r.NombreArchivo, r.Tamano, r.FechaHora, r.IdAdmin))
            .ToListAsync(ct);

    public async Task<int> CrearRespaldoAsync(string nombreArchivo, string tamano, int idAdmin, CancellationToken ct = default)
    {
        var row = new RespaldoRow
        {
            NombreArchivo = nombreArchivo,
            Tamano = tamano,
            FechaHora = DateTime.UtcNow,
            IdAdmin = idAdmin
        };
        db.Respaldos.Add(row);
        await db.SaveChangesAsync(ct);
        return row.IdRespaldo;
    }

    public async Task<RespaldoDto?> FindRespaldoByIdAsync(int idRespaldo, CancellationToken ct = default)
        => await db.Respaldos.AsNoTracking()
            .Where(r => r.IdRespaldo == idRespaldo)
            .Select(r => new RespaldoDto(r.IdRespaldo, r.NombreArchivo, r.Tamano, r.FechaHora, r.IdAdmin))
            .FirstOrDefaultAsync(ct);

    // Corrección BD: sp_mantenimiento_limpieza hace las 3 limpiezas en 1 llamada.
    public async Task<MantenimientoLimpiezaResult> LimpiarTodoAsync(int dias, CancellationToken ct = default)
    {
        var r = await db.Database.SqlQueryRaw<MantenimientoLimpiezaResult>(
            "EXEC dbo.sp_mantenimiento_limpieza @dias",
            new SqlParameter("@dias", dias)).ToListAsync(ct);
        return r.Single();
    }

    // Snapshot JSON de datos (agnóstico SQL/NoSQL). Se usa como respaldo de contingencia
    // si BACKUP DATABASE nativo falla (permisos/carpeta). Nunca expone secrets.
    public async Task<string> ExportSnapshotJsonAsync(CancellationToken ct = default)
    {
        var snapshot = new
        {
            sistema = "Mindzyx",
            tipo = "respaldo-datos",
            version = 1,
            exportadoEn = DateTime.UtcNow,
            data = new
            {
                roles = await db.Roles.AsNoTracking().ToListAsync(ct),
                generos = await db.Generos.AsNoTracking().ToListAsync(ct),
                estados = await db.Estados.AsNoTracking().ToListAsync(ct),
                usuarios = await db.Usuarios.AsNoTracking().ToListAsync(ct)
            }
        };
        return System.Text.Json.JsonSerializer.Serialize(snapshot, new System.Text.Json.JsonSerializerOptions { WriteIndented = true });
    }

    // BACKUP DATABASE nativo de SQL Server usando la credencial dedicada (db_backupoperator).
    // El archivo lo escribe el motor de SQL Server; la carpeta destino debe ser escribible
    // por el usuario de servicio del motor y legible por el proceso de la app (descarga).
    public async Task CreateSqlBackupAsync(string rutaArchivo, CancellationToken ct = default)
    {
        var cs = adminOptions.Value.BackupConnectionString;
        if (string.IsNullOrWhiteSpace(cs))
            throw new InvalidOperationException("BackupConnectionString no configurada.");

        await using var conn = new SqlConnection(cs);
        await conn.OpenAsync(ct);
        await using var cmd = conn.CreateCommand();
        cmd.CommandTimeout = 0;
        cmd.CommandText = "BACKUP DATABASE [Mindzyxs] TO DISK = @ruta WITH FORMAT, INIT, COMPRESSION";
        cmd.Parameters.AddWithValue("@ruta", rutaArchivo);
        await cmd.ExecuteNonQueryAsync(ct);
    }
}
