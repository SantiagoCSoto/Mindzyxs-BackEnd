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
        // Sin OUTPUT: dbo.usuarios tiene INSTEAD OF INSERT (TR_usuarios_PasswordSeguro)
        // y SQL Server rechaza INSERT...OUTPUT con triggers (error 334).
        var ids = await db.Database.SqlQueryRaw<int>(
            "INSERT INTO dbo.usuarios (nombre, apellido, correo, password_hash, fecha_nacimiento, id_rol, id_estado_cuenta, id_estado_aprobacion, id_genero, fecha_registro) VALUES (@n, @a, @c, @p, @fn, @r, @ec, @ea, @g, SYSDATETIME()); SELECT CAST(SCOPE_IDENTITY() AS INT)",
            new SqlParameter("@n", user.Nombre.Trim()),
            new SqlParameter("@a", user.Apellido.Trim()),
            new SqlParameter("@c", ContactHelper.Normalize(user.Correo)),
            new SqlParameter("@p", user.PasswordHash),
            new SqlParameter("@fn", (object?)user.FechaNacimiento ?? DBNull.Value),
            new SqlParameter("@r", user.IdRol),
            new SqlParameter("@ec", user.IdEstadoCuenta),
            new SqlParameter("@ea", (object?)user.IdEstadoAprobacion ?? DBNull.Value),
            new SqlParameter("@g", (object?)user.IdGenero ?? DBNull.Value)).ToListAsync(ct);
        return ids.Single();
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

    // PB-10 HU1-PB10: actualiza nombres, apellidos, fecha de nacimiento y género.
    public async Task ActualizarPerfilAsync(int idUsuario, string nombres, string apellidos, DateTime? fechaNacimiento, int? idGenero, CancellationToken ct = default)
        => await db.Usuarios.Where(x => x.IdUsuario == idUsuario)
            .ExecuteUpdateAsync(s => s
                .SetProperty(x => x.Nombre, nombres)
                .SetProperty(x => x.Apellido, apellidos)
                .SetProperty(x => x.FechaNacimiento, fechaNacimiento)
                .SetProperty(x => x.IdGenero, idGenero), ct);

    // PB-10 HU1-PB10: cambia el correo (ya verificado por código).
    public async Task ActualizarCorreoAsync(int idUsuario, string nuevoCorreo, CancellationToken ct = default)
        => await db.Usuarios.Where(x => x.IdUsuario == idUsuario)
            .ExecuteUpdateAsync(s => s.SetProperty(x => x.Correo, nuevoCorreo), ct);

    // PB-25 HU1-PB25: gestión de usuarios (excluye admins; filtros q/rol/estado).
    public async Task<List<UsuarioAdminRow>> GetUsuariosAsync(int idRolAdmin, string? q, int? idRol, int? idEstadoCuenta, CancellationToken ct = default)
    {
        var query = db.Usuarios.AsNoTracking().Where(u => u.IdRol != idRolAdmin);
        if (idRol.HasValue)
            query = query.Where(u => u.IdRol == idRol.Value);
        if (idEstadoCuenta.HasValue)
            query = query.Where(u => u.IdEstadoCuenta == idEstadoCuenta.Value);
        if (!string.IsNullOrWhiteSpace(q))
        {
            var t = q.Trim().ToLower();
            query = query.Where(u => (u.Nombre + " " + u.Apellido).ToLower().Contains(t) || u.Correo.ToLower().Contains(t));
        }
        var filas = await query.OrderByDescending(u => u.FechaRegistro).ToListAsync(ct);
        if (filas.Count == 0) return [];
        var roles = await db.Roles.AsNoTracking().ToDictionaryAsync(r => r.IdRol, r => r.Nombre, ct);
        var estados = await db.Estados.AsNoTracking().ToDictionaryAsync(e => e.IdEstado, e => e.Nombre, ct);
        return filas.Select(u => new UsuarioAdminRow(
            u.IdUsuario, u.Nombre, u.Apellido, u.Correo, u.IdRol,
            roles.TryGetValue(u.IdRol, out var r) ? r : "",
            u.IdEstadoCuenta,
            estados.TryGetValue(u.IdEstadoCuenta, out var e) ? e : "",
            u.IdEstadoAprobacion,
            u.IdEstadoAprobacion.HasValue && estados.TryGetValue(u.IdEstadoAprobacion.Value, out var a) ? a : null,
            u.FechaRegistro)).ToList();
    }

    // PB-especialidades: reemplaza las especialidades del psicólogo (vacío = ninguna).
    public async Task GuardarEspecialidadesAsync(int idPsicologo, List<int> idsEspecialidad, CancellationToken ct = default)
    {
        var actuales = db.PsicologoEspecialidades.Where(p => p.IdPsicologo == idPsicologo);
        db.PsicologoEspecialidades.RemoveRange(actuales);
        foreach (var id in idsEspecialidad.Distinct())
            db.PsicologoEspecialidades.Add(new PsicologoEspecialidadRow { IdPsicologo = idPsicologo, IdEspecialidad = id, Verificada = true });
        await db.SaveChangesAsync(ct);
    }

    // PB-25: reemplaza conservando verificación; las agregadas quedan ocultas (false).
    public async Task<bool> ActualizarEspecialidadesAsync(int idPsicologo, List<int> idsEspecialidad, CancellationToken ct = default)
    {
        var nuevas = idsEspecialidad.Distinct().ToHashSet();
        var actuales = await db.PsicologoEspecialidades
            .Where(p => p.IdPsicologo == idPsicologo)
            .ToListAsync(ct);
        var actualesIds = actuales.Select(p => p.IdEspecialidad).ToHashSet();
        if (actualesIds.SetEquals(nuevas)) return false;

        db.PsicologoEspecialidades.RemoveRange(actuales.Where(p => !nuevas.Contains(p.IdEspecialidad)));
        foreach (var id in nuevas.Where(i => !actualesIds.Contains(i)))
            db.PsicologoEspecialidades.Add(new PsicologoEspecialidadRow { IdPsicologo = idPsicologo, IdEspecialidad = id, Verificada = false });
        await db.SaveChangesAsync(ct);
        return true;
    }

    public async Task SetTarjetaVerificadaAsync(int idPsicologo, bool verificada, CancellationToken ct = default)
        => await db.Usuarios.Where(x => x.IdUsuario == idPsicologo)
            .ExecuteUpdateAsync(s => s.SetProperty(x => x.TarjetaProfesionalVerificada, verificada), ct);

    public async Task<List<string>> GetEspecialidadesPendientesAsync(int idPsicologo, CancellationToken ct = default)
        => await (from pe in db.PsicologoEspecialidades.AsNoTracking()
                  join e in db.Especialidades.AsNoTracking() on pe.IdEspecialidad equals e.IdEspecialidad
                  where pe.IdPsicologo == idPsicologo && !pe.Verificada
                  orderby e.Nombre
                  select e.Nombre).ToListAsync(ct);

    // PB-25: verifica las pendientes del psicólogo. Devuelve cuántas verificó.
    public async Task<int> VerificarEspecialidadesAsync(int idPsicologo, CancellationToken ct = default)
        => await db.PsicologoEspecialidades
            .Where(p => p.IdPsicologo == idPsicologo && !p.Verificada)
            .ExecuteUpdateAsync(s => s.SetProperty(p => p.Verificada, true), ct);

    // PB-25: psicólogos con tarjeta sin verificar o especialidades pendientes (cola del admin).
    public async Task<List<PsicologoTarjetaPendienteRow>> GetPsicologosTarjetaPendienteAsync(int idRolPsicologo, CancellationToken ct = default)
    {
        var pendientes = await db.PsicologoEspecialidades.AsNoTracking()
            .Where(p => !p.Verificada)
            .GroupBy(p => p.IdPsicologo)
            .Select(g => new { Id = g.Key, N = g.Count() })
            .ToDictionaryAsync(g => g.Id, g => g.N, ct);
        var conPendientes = pendientes.Keys.ToList();
        var usuarios = await db.Usuarios.AsNoTracking()
            .Where(u => u.IdRol == idRolPsicologo
                && (!u.TarjetaProfesionalVerificada || conPendientes.Contains(u.IdUsuario)))
            .OrderBy(u => u.Nombre).ThenBy(u => u.Apellido)
            .Select(u => new { u.IdUsuario, u.Nombre, u.Apellido, u.Correo, u.TarjetaProfesionalVerificada })
            .ToListAsync(ct);
        return usuarios.Select(u => new PsicologoTarjetaPendienteRow(
                u.IdUsuario, $"{u.Nombre} {u.Apellido}".Trim(), u.Correo,
                u.TarjetaProfesionalVerificada,
                pendientes.TryGetValue(u.IdUsuario, out var n) ? n : 0))
            .ToList();
    }

    // PB-especialidades: directorio para pacientes (activas y aprobadas, con conteo de asignados).
    // Filtros opcionales: q (nombre/apellido) e idEspecialidad (checklist).
    public async Task<List<PsicologoDirectorioRow>> GetDirectorioPsicologosAsync(int idRolPsicologo, int idEstadoActiva, int idEstadoAprobado, int idEstadoAutorizado, string? q, int? idEspecialidad, CancellationToken ct = default)
    {
        var psicos = await db.Usuarios.AsNoTracking()
            .Where(u => u.IdRol == idRolPsicologo && u.IdEstadoCuenta == idEstadoActiva
                && (u.IdEstadoAprobacion == null || u.IdEstadoAprobacion == idEstadoAprobado)
                && (string.IsNullOrWhiteSpace(q) || u.Nombre.Contains(q) || u.Apellido.Contains(q))
                && (!idEspecialidad.HasValue || db.PsicologoEspecialidades.Any(pe => pe.IdPsicologo == u.IdUsuario && pe.IdEspecialidad == idEspecialidad.Value && pe.Verificada)))
            .OrderBy(u => u.Nombre).ThenBy(u => u.Apellido)
            .Select(u => new { u.IdUsuario, u.Nombre, u.Apellido })
            .ToListAsync(ct);
        if (psicos.Count == 0) return [];
        var ids = psicos.Select(p => p.IdUsuario).ToList();
        var esp = await (from pe in db.PsicologoEspecialidades.AsNoTracking()
                         join e in db.Especialidades.AsNoTracking() on pe.IdEspecialidad equals e.IdEspecialidad
                         where ids.Contains(pe.IdPsicologo) && pe.Verificada
                         select new { pe.IdPsicologo, e.Nombre }).ToListAsync(ct);
        var conteos = await db.Autorizaciones.AsNoTracking()
            .Where(a => ids.Contains(a.IdPsicologo) && a.IdEstado == idEstadoAutorizado)
            .GroupBy(a => a.IdPsicologo)
            .Select(g => new { Id = g.Key, N = g.Select(a => a.IdPaciente).Distinct().Count() })
            .ToDictionaryAsync(g => g.Id, g => g.N, ct);
        var porPsicologo = esp.GroupBy(x => x.IdPsicologo)
            .ToDictionary(g => g.Key, g => g.Select(x => x.Nombre).ToList());
        return psicos.Select(p => new PsicologoDirectorioRow(
            p.IdUsuario,
            $"{p.Nombre} {p.Apellido}".Trim(),
            porPsicologo.TryGetValue(p.IdUsuario, out var lista) ? lista : [],
            conteos.TryGetValue(p.IdUsuario, out var n) ? n : 0)).ToList();
    }

    // PB-24 HU1-PB24: perfil profesional del psicólogo.
    public async Task<UsuarioDto?> FindPerfilProfesionalAsync(int idUsuario, CancellationToken ct = default)
    {
        var u = await db.Usuarios.AsNoTracking()
            .Where(x => x.IdUsuario == idUsuario)
            .Select(x => new
            {
                x.IdUsuario,
                x.IdRol,
                x.IdEstadoCuenta,
                x.IdEstadoAprobacion,
                x.Nombre,
                x.Apellido,
                x.Correo,
                x.PasswordHash,
                x.FechaNacimiento,
                x.IdGenero,
                x.FilePath,
                x.AñosExperiencia,
                x.DescripcionProfesional,
                x.DatosContacto,
                x.TarjetaProfesionalVerificada
            })
            .FirstOrDefaultAsync(ct);

        if (u is null) return null;

        var dto = new UsuarioDto
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
            FilePath = u.FilePath,
            AñosExperiencia = u.AñosExperiencia,
            DescripcionProfesional = u.DescripcionProfesional,
            DatosContacto = u.DatosContacto,
            TarjetaProfesionalVerificada = u.TarjetaProfesionalVerificada
        };

        // Cargar especialidades
        var esp = await db.PsicologoEspecialidades.AsNoTracking()
            .Where(p => p.IdPsicologo == idUsuario)
            .Join(db.Especialidades.AsNoTracking(), p => p.IdEspecialidad, e => e.IdEspecialidad, (p, e) => e.Nombre)
            .ToListAsync(ct);
        dto.Especialidades = esp;
        dto.EspecialidadesPendientes = await GetEspecialidadesPendientesAsync(idUsuario, ct);

        return dto;
    }

    // PB-24 HU1-PB24: actualiza perfil profesional del psicólogo.
    // PB-25: especialidades con diff (agregadas quedan ocultas); si cambiaron,
    // la tarjeta pasa a no verificada. Devuelve true si hubo cambios.
    // La verificación en true solo la pone el admin (VerificarEspecialidadesAsync).
    public async Task<bool> ActualizarPerfilProfesionalAsync(int idUsuario, int? añosExperiencia, string? descripcionProfesional, string? datosContacto, List<int> idsEspecialidad, CancellationToken ct = default)
    {
        await db.Usuarios.Where(x => x.IdUsuario == idUsuario)
            .ExecuteUpdateAsync(s => s
                .SetProperty(x => x.AñosExperiencia, añosExperiencia)
                .SetProperty(x => x.DescripcionProfesional, descripcionProfesional)
                .SetProperty(x => x.DatosContacto, datosContacto), ct);

        var huboCambios = await ActualizarEspecialidadesAsync(idUsuario, idsEspecialidad, ct);
        if (huboCambios)
            await SetTarjetaVerificadaAsync(idUsuario, false, ct);
        return huboCambios;
    }

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
        FechaRegistro = u.FechaRegistro,
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

    // PB-especialidades: checklist para el registro del psicólogo.
    public async Task<List<CatalogItemDto>> GetEspecialidadesAsync(CancellationToken ct = default)
        => await db.Especialidades.AsNoTracking().OrderBy(e => e.Nombre)
            .Select(e => new CatalogItemDto { Id = e.IdEspecialidad, Codigo = e.Nombre, Nombre = e.Nombre })
            .ToListAsync(ct);

    public async Task<bool> ExisteEspecialidadAsync(int idEspecialidad, CancellationToken ct = default)
        => await db.Especialidades.AnyAsync(e => e.IdEspecialidad == idEspecialidad, ct);

    public async Task<string?> GetRolNombreAsync(int idRol, CancellationToken ct = default)
        => await db.Roles.Where(r => r.IdRol == idRol).Select(r => r.Nombre).FirstOrDefaultAsync(ct);

    public async Task<string?> GetEstadoNombreAsync(int idEstado, CancellationToken ct = default)
        => await db.Estados.Where(e => e.IdEstado == idEstado).Select(e => e.Nombre).FirstOrDefaultAsync(ct);
}

public class EfCodigoRepository(MindzyxsDbContext db) : ICodigoRepository
{
    // Sin OUTPUT: dbo.codigos_verificacion tiene AFTER INSERT (TR_codigos_ConsumoUnico)
    // y SQL Server rechaza INSERT...OUTPUT con triggers (error 334).
    public async Task CrearAsync(int idUsuario, string codigo, string tipo, DateTime expiraEn, CancellationToken ct = default)
    {
        await db.Database.ExecuteSqlRawAsync(
            "INSERT INTO dbo.codigos_verificacion (id_usuario, codigo, tipo, expira_en, creado_en) VALUES (@u, @c, @t, @e, SYSDATETIME())",
            new object[]
            {
                new SqlParameter("@u", idUsuario),
                new SqlParameter("@c", codigo),
                new SqlParameter("@t", tipo),
                new SqlParameter("@e", expiraEn)
            },
            ct);
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

    // Seguridad: revoca todas las sesiones del usuario (ej. al restablecer contraseña).
    public async Task DeleteByUsuarioAsync(int idUsuario, CancellationToken ct = default)
        => await db.Sesiones.Where(s => s.IdUsuario == idUsuario).ExecuteDeleteAsync(ct);
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

// PB-13: historial emocional (lectura; el paciente solo ve lo suyo, lo valida el service).
public class EfHistorialRepository(MindzyxsDbContext db) : IHistorialRepository
{
    public Task<List<EventoRow>> GetEventosAsync(int idPaciente, DateTime? desde, DateTime? hasta, CancellationToken ct = default)
        => db.Eventos.AsNoTracking()
            .Where(e => e.IdPaciente == idPaciente
                && (!desde.HasValue || e.FechaEvento >= desde.Value)
                && (!hasta.HasValue || e.FechaEvento <= hasta.Value))
            .OrderByDescending(e => e.FechaEvento)
            .ToListAsync(ct);

    public Task<EventoRow?> FindEventoAsync(int idEvento, CancellationToken ct = default)
        => db.Eventos.AsNoTracking()
            .Where(e => e.IdEvento == idEvento)
            .FirstOrDefaultAsync(ct);

    public Task<List<RespuestaRow>> GetRespuestasAsync(int idEvento, CancellationToken ct = default)
        => db.Respuestas.AsNoTracking()
            .Where(r => r.IdEvento == idEvento)
            .OrderBy(r => r.IdRespuesta)
            .ToListAsync(ct);

    // Textos de opción por (pregunta, valor): respaldo para respuestas viejas sin texto.
    // Tolerante a opciones duplicadas (misma pregunta y valor): toma la primera.
    public async Task<Dictionary<(int IdPregunta, int Valor), string>> GetTextosOpcionAsync(List<int> idsPregunta, CancellationToken ct = default)
    {
        if (idsPregunta.Count == 0) return new();
        var filas = await db.OpcionesPregunta.AsNoTracking()
            .Where(o => idsPregunta.Contains(o.IdPregunta))
            .ToListAsync(ct);
        return filas.GroupBy(o => (o.IdPregunta, o.Valor))
            .ToDictionary(g => g.Key, g => g.First().Texto);
    }

    public Task<List<ObservacionRow>> GetObservacionesAsync(int idEvento, CancellationToken ct = default)
        => db.Observaciones.AsNoTracking()
            .Where(o => o.IdEvento == idEvento)
            .OrderBy(o => o.FechaObservacion)
            .ToListAsync(ct);

    public async Task<Dictionary<int, string>> GetPreguntasTextoAsync(List<int> idsPregunta, CancellationToken ct = default)
        => idsPregunta.Count == 0
            ? new()
            : await db.Preguntas.AsNoTracking()
                .Where(p => idsPregunta.Contains(p.IdPregunta))
                .ToDictionaryAsync(p => p.IdPregunta, p => p.Texto, ct);

    public async Task<Dictionary<int, string>> GetNivelesAsync(CancellationToken ct = default)
        => await db.NivelesRiesgo.AsNoTracking()
            .ToDictionaryAsync(n => n.IdNivelRiesgo, n => n.Nombre, ct);

    public async Task<Dictionary<int, string>> GetCuestionariosTituloAsync(List<int> ids, CancellationToken ct = default)
        => ids.Count == 0
            ? new()
            : await db.Cuestionarios.AsNoTracking()
                .Where(c => ids.Contains(c.IdCuestionario))
                .ToDictionaryAsync(c => c.IdCuestionario, c => c.Titulo, ct);

    // PB-14: pacientes que autorizaron al psicólogo (idEstadoAutorizado = Autorizado).
    public async Task<List<UsuarioDto>> GetPacientesAutorizadosAsync(int idPsicologo, int idEstadoAutorizado, CancellationToken ct = default)
    {
        var ids = await db.Autorizaciones.AsNoTracking()
            .Where(a => a.IdPsicologo == idPsicologo && a.IdEstado == idEstadoAutorizado)
            .Select(a => a.IdPaciente)
            .Distinct()
            .ToListAsync(ct);
        if (ids.Count == 0) return [];
        return await db.Usuarios.AsNoTracking()
            .Where(u => ids.Contains(u.IdUsuario))
            .OrderBy(u => u.Nombre).ThenBy(u => u.Apellido)
            .Select(u => new UsuarioDto
            {
                IdUsuario = u.IdUsuario,
                Nombre = u.Nombre,
                Apellido = u.Apellido,
                Correo = u.Correo,
                IdRol = u.IdRol,
                IdEstadoCuenta = u.IdEstadoCuenta
            })
            .ToListAsync(ct);
    }

    // PB-17: ids de psicólogos autorizados por el paciente.
    public async Task<List<int>> GetPsicologosAutorizadosAsync(int idPaciente, int idEstadoAutorizado, CancellationToken ct = default)
        => await db.Autorizaciones.AsNoTracking()
            .Where(a => a.IdPaciente == idPaciente && a.IdEstado == idEstadoAutorizado)
            .Select(a => a.IdPsicologo)
            .Distinct()
            .ToListAsync(ct);

    public Task<bool> EstaAutorizadoAsync(int idPaciente, int idPsicologo, int idEstadoAutorizado, CancellationToken ct = default)
        => db.Autorizaciones.AsNoTracking()
            .Where(a => a.IdPaciente == idPaciente && a.IdPsicologo == idPsicologo && a.IdEstado == idEstadoAutorizado)
            .AnyAsync(ct);

    // PB-31: aceptar el chat otorga autorización (upsert a Autorizado).
    public async Task AsegurarAutorizacionAsync(int idPaciente, int idPsicologo, int idEstadoAutorizado, CancellationToken ct = default)
    {
        var existente = await db.Autorizaciones
            .FirstOrDefaultAsync(a => a.IdPaciente == idPaciente && a.IdPsicologo == idPsicologo, ct);
        if (existente is null)
        {
            db.Autorizaciones.Add(new AutorizacionRow
            {
                IdPaciente = idPaciente,
                IdPsicologo = idPsicologo,
                IdEstado = idEstadoAutorizado,
                FechaSolicitud = DateTime.UtcNow,
                FechaRespuesta = DateTime.UtcNow
            });
        }
        else
        {
            existente.IdEstado = idEstadoAutorizado;
            existente.FechaRespuesta = DateTime.UtcNow;
        }
        await db.SaveChangesAsync(ct);
    }

    // PB-17: revoca el acceso (pasa a Revocado con fecha de respuesta).
    public async Task RevocarAutorizacionAsync(int idPaciente, int idPsicologo, int idEstadoRevocado, CancellationToken ct = default)
    {
        var existente = await db.Autorizaciones
            .FirstOrDefaultAsync(a => a.IdPaciente == idPaciente && a.IdPsicologo == idPsicologo, ct);
        if (existente is null)
        {
            db.Autorizaciones.Add(new AutorizacionRow
            {
                IdPaciente = idPaciente,
                IdPsicologo = idPsicologo,
                IdEstado = idEstadoRevocado,
                FechaSolicitud = DateTime.UtcNow,
                FechaRespuesta = DateTime.UtcNow
            });
        }
        else
        {
            existente.IdEstado = idEstadoRevocado;
            existente.FechaRespuesta = DateTime.UtcNow;
        }
        await db.SaveChangesAsync(ct);
    }

    public async Task<int?> GetEstadoIdAsync(string nombre, CancellationToken ct = default)
        => await db.Estados.AsNoTracking()
            .Where(e => e.Nombre == nombre)
            .Select(e => (int?)e.IdEstado)
            .FirstOrDefaultAsync(ct);

    // PB-11 HU1-PB11: ids de los cuestionarios que el paciente ya respondió (eventos
    // tipo Cuestionario), para armar el pendiente con rotación igual que el invitado.
    public async Task<List<int>> GetCuestionariosRespondidosAsync(int idPaciente, CancellationToken ct = default)
    {
        var idTipoCuestionario = await GetTipoEventoIdAsync("Cuestionario", ct);
        return await db.Eventos.AsNoTracking()
            .Where(e => e.IdPaciente == idPaciente && e.IdTipoEvento == (idTipoCuestionario ?? -1) && e.IdCuestionario.HasValue)
            .Select(e => e.IdCuestionario!.Value)
            .Distinct()
            .ToListAsync(ct);
    }

    public async Task<int?> GetNivelIdAsync(string nombre, CancellationToken ct = default)
        => await db.NivelesRiesgo.AsNoTracking()
            .Where(n => n.Nombre == nombre)
            .Select(n => (int?)n.IdNivelRiesgo)
            .FirstOrDefaultAsync(ct);

    public async Task<int?> GetTipoEventoIdAsync(string nombre, CancellationToken ct = default)
        => await db.TiposEvento.AsNoTracking()
            .Where(t => t.Nombre == nombre)
            .Select(t => (int?)t.IdTipoEvento)
            .FirstOrDefaultAsync(ct);

    // PB-11 HU1-PB11: guarda el evento del cuestionario con sus respuestas en una sola
    // transacción (eventos_bienestar + respuestas). Devuelve id_evento.
    public async Task<int> GuardarEventoCuestionarioAsync(int idPaciente, int idCuestionario, int idNivelRiesgo, int puntajeTotal, List<RespuestaRow> respuestas, CancellationToken ct = default)
    {
        var idTipoCuestionario = await GetTipoEventoIdAsync("Cuestionario", ct);
        if (idTipoCuestionario is null)
            throw new InvalidOperationException("Tipo de evento Cuestionario no configurado.");

        await using var tx = await db.Database.BeginTransactionAsync(ct);
        var evento = new EventoRow
        {
            IdPaciente = idPaciente,
            IdTipoEvento = idTipoCuestionario.Value,
            IdCuestionario = idCuestionario,
            IdNivelRiesgo = idNivelRiesgo,
            PuntajeTotal = puntajeTotal,
            FechaEvento = DateTime.UtcNow
        };
        db.Eventos.Add(evento);
        await db.SaveChangesAsync(ct);

        foreach (var r in respuestas)
        {
            db.Respuestas.Add(new RespuestaRow
            {
                IdEvento = evento.IdEvento,
                IdPregunta = r.IdPregunta,
                Valor = r.Valor,
                Texto = r.Texto,
                FechaRespuesta = evento.FechaEvento
            });
        }
        await db.SaveChangesAsync(ct);
        await tx.CommitAsync(ct);

        return evento.IdEvento;
    }

    // PB-12 HU1-PB12: guarda el registro del estado emocional (escala 1-5 + nota opcional)
    // en dbo.eventos_bienestar. La fecha/hora la asigna el service (automática).
    public async Task<int> GuardarEstadoEmocionalAsync(int idPaciente, int idTipoEvento, int idNivelRiesgo, int puntaje, string? nota, DateTime fechaEvento, CancellationToken ct = default)
    {
        var evento = new EventoRow
        {
            IdPaciente = idPaciente,
            IdTipoEvento = idTipoEvento,
            IdNivelRiesgo = idNivelRiesgo,
            PuntajeTotal = puntaje,
            Nota = nota,
            FechaEvento = fechaEvento
        };
        db.Eventos.Add(evento);
        await db.SaveChangesAsync(ct);
        return evento.IdEvento;
    }

    // PB-19: guarda la observación del psicólogo (fecha/hora automática).
    public async Task<int> GuardarObservacionAsync(int idPsicologo, int idPaciente, int? idEvento, string contenido, string firma, CancellationToken ct = default)
    {
        var obs = new ObservacionRow
        {
            IdPsicologo = idPsicologo,
            IdPaciente = idPaciente,
            IdEvento = idEvento,
            Contenido = contenido,
            Firma = firma,
            FechaObservacion = DateTime.UtcNow
        };
        db.Observaciones.Add(obs);
        await db.SaveChangesAsync(ct);
        return obs.IdObservacion;
    }
}

// PB-15: recordatorios (upsert por paciente + reemplazo de días).
public class EfRecordatorioRepository(MindzyxsDbContext db) : IRecordatorioRepository
{
    public Task<RecordatorioRow?> GetDePacienteAsync(int idPaciente, CancellationToken ct = default)
        => db.Recordatorios.AsNoTracking()
            .Where(r => r.IdPaciente == idPaciente)
            .FirstOrDefaultAsync(ct);

    public async Task<List<int>> GetDiasAsync(int idRecordatorio, CancellationToken ct = default)
        => await db.RecordatorioDias.AsNoTracking()
            .Where(d => d.IdRecordatorio == idRecordatorio)
            .OrderBy(d => d.IdDia)
            .Select(d => d.IdDia)
            .ToListAsync(ct);

    public async Task<RecordatorioRow> GuardarAsync(int idPaciente, string titulo, string? mensaje, TimeSpan hora, string frecuencia, bool activo, List<int> dias, CancellationToken ct = default)
    {
        var row = await db.Recordatorios
            .Where(r => r.IdPaciente == idPaciente)
            .FirstOrDefaultAsync(ct);
        if (row is null)
        {
            row = new RecordatorioRow { IdPaciente = idPaciente };
            db.Recordatorios.Add(row);
        }
        row.Titulo = titulo;
        row.Mensaje = mensaje;
        row.Hora = hora;
        row.Frecuencia = frecuencia;
        row.Activo = activo;
        await db.SaveChangesAsync(ct);

        var viejos = await db.RecordatorioDias
            .Where(d => d.IdRecordatorio == row.IdRecordatorio)
            .ToListAsync(ct);
        db.RecordatorioDias.RemoveRange(viejos);
        foreach (var dia in dias.Distinct().Where(d => d is >= 1 and <= 7))
            db.RecordatorioDias.Add(new RecordatorioDiaRow { IdRecordatorio = row.IdRecordatorio, IdDia = dia });
        await db.SaveChangesAsync(ct);
        return row;
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

    // PB-22: asignaciones del paciente con datos del cuestionario, recientes primero.
    public Task<List<AsignacionCuestionarioDto>> GetAsignadosPorPacienteAsync(int idPaciente, CancellationToken ct = default)
        => (from a in db.Asignaciones.AsNoTracking()
            join c in db.Cuestionarios.AsNoTracking() on a.IdCuestionario equals c.IdCuestionario
            where a.IdPaciente == idPaciente
            orderby a.FechaAsignacion descending
            select new AsignacionCuestionarioDto(
                a.IdAsignacion, a.IdCuestionario, c.Titulo, c.Tipo,
                a.IdPaciente, a.IdPsicologo, a.FechaAsignacion, a.FechaLimite, a.Estado))
            .ToListAsync(ct);

    // PB-22: asignación pendiente del paciente para un cuestionario (cualquiera de sus estados pendientes).
    public Task<AsignacionCuestionarioDto?> FindAsignacionPendienteAsync(int idPaciente, int idCuestionario, CancellationToken ct = default)
        => (from a in db.Asignaciones.AsNoTracking()
            join c in db.Cuestionarios.AsNoTracking() on a.IdCuestionario equals c.IdCuestionario
            where a.IdPaciente == idPaciente && a.IdCuestionario == idCuestionario
                && (a.Estado == "Pendiente" || a.Estado == "Asignado")
            orderby a.FechaAsignacion descending
            select new AsignacionCuestionarioDto(
                a.IdAsignacion, a.IdCuestionario, c.Titulo, c.Tipo,
                a.IdPaciente, a.IdPsicologo, a.FechaAsignacion, a.FechaLimite, a.Estado))
            .FirstOrDefaultAsync(ct);

    // PB-22: al finalizar, la asignación queda Completada (automático).
    public async Task MarcarAsignacionCompletadaAsync(int idAsignacion, CancellationToken ct = default)
        => await db.Asignaciones.Where(a => a.IdAsignacion == idAsignacion)
            .ExecuteUpdateAsync(s => s.SetProperty(a => a.Estado, "Completada"), ct);

    // PB-22: el psicólogo asigna su cuestionario a los pacientes autorizados. Crea una
    // fila por paciente (Pendiente) y evita duplicar si ya existe una asignación
    // Pendiente del mismo cuestionario para ese paciente. Devuelve los ids creados.
    public async Task<List<int>> AsignarAsync(int idCuestionario, int idPsicologo, List<int> idsPacientes, CancellationToken ct = default)
    {
        var idsUnicos = idsPacientes.Distinct().ToList();
        var existentes = await db.Asignaciones.AsNoTracking()
            .Where(a => a.IdCuestionario == idCuestionario
                && (a.Estado == "Pendiente" || a.Estado == "Asignado")
                && idsUnicos.Contains(a.IdPaciente))
            .Select(a => a.IdPaciente)
            .ToListAsync(ct);

        var aInsertar = idsUnicos.Except(existentes).ToList();
        if (aInsertar.Count == 0) return [];

        var filas = aInsertar.Select(idPac => new AsignacionRow
        {
            IdCuestionario = idCuestionario,
            IdPaciente = idPac,
            IdPsicologo = idPsicologo,
            Estado = "Pendiente"
        }).ToList();

        db.Asignaciones.AddRange(filas);
        await db.SaveChangesAsync(ct);
        return filas.Select(f => f.IdAsignacion).ToList();
    }

    // PB-22: pacientes con asignación Pendiente/Asignado de un cuestionario (para
    // deshabilitarlos en la pantalla de asignación).
    public Task<List<int>> GetPacientesConAsignacionPendienteAsync(int idCuestionario, CancellationToken ct = default)
        => db.Asignaciones.AsNoTracking()
            .Where(a => a.IdCuestionario == idCuestionario
                && (a.Estado == "Pendiente" || a.Estado == "Asignado"))
            .Select(a => a.IdPaciente)
            .Distinct()
            .ToListAsync(ct);

    public Task<CuestionarioRow?> FindByIdAsync(int idCuestionario, CancellationToken ct = default)
        => db.Cuestionarios.AsNoTracking()
            .Where(c => c.IdCuestionario == idCuestionario)
            .FirstOrDefaultAsync(ct);

    // PB-20: crea el cuestionario con preguntas y opciones en una transacción.
    public async Task<int> CrearAsync(CuestionarioRow cuestionario, List<(PreguntaRow Pregunta, List<OpcionPreguntaRow> Opciones)> preguntas, CancellationToken ct = default)
    {
        await using var tx = await db.Database.BeginTransactionAsync(ct);
        try
        {
            cuestionario.FechaCreacion = DateTime.UtcNow;
            db.Cuestionarios.Add(cuestionario);
            await db.SaveChangesAsync(ct);
            foreach (var (pregunta, opciones) in preguntas)
            {
                pregunta.IdCuestionario = cuestionario.IdCuestionario;
                db.Preguntas.Add(pregunta);
                await db.SaveChangesAsync(ct);
                foreach (var op in opciones)
                {
                    op.IdPregunta = pregunta.IdPregunta;
                    db.OpcionesPregunta.Add(op);
                }
                await db.SaveChangesAsync(ct);
            }
            await tx.CommitAsync(ct);
            return cuestionario.IdCuestionario;
        }
        catch
        {
            await tx.RollbackAsync(ct);
            throw;
        }
    }

    // PB-21: lista "Mis cuestionarios" del psicólogo (Borrador/Publicado), con búsqueda
    // por título y recientes primero.
    public Task<List<CuestionarioPsicologoListaDto>> GetDePsicologoAsync(int idPsicologo, string? q, CancellationToken ct = default)
        => (from c in db.Cuestionarios.AsNoTracking()
            where c.IdCreador == idPsicologo && (c.Estado == "Borrador" || c.Estado == "Publicado")
                && (string.IsNullOrEmpty(q) || c.Titulo.Contains(q))
            orderby c.FechaCreacion descending
            select new CuestionarioPsicologoListaDto(
                c.IdCuestionario, c.Titulo, c.Descripcion, c.Estado,
                db.Preguntas.Count(p => p.IdCuestionario == c.IdCuestionario && !p.Eliminada),
                c.FechaCreacion))
            .ToListAsync(ct);

    // PB-21: detalle de un cuestionario propio (solo Borrador/Publicado; no Eliminado).
    public Task<CuestionarioRow?> FindDePsicologoAsync(int idCuestionario, int idPsicologo, CancellationToken ct = default)
        => db.Cuestionarios.AsNoTracking()
            .Where(c => c.IdCuestionario == idCuestionario && c.IdCreador == idPsicologo
                && (c.Estado == "Borrador" || c.Estado == "Publicado"))
            .FirstOrDefaultAsync(ct);

    // PB-21: pasa el cuestionario a Borrador (confirmación del psicólogo antes de editar).
    public async Task DespublicarAsync(int idCuestionario, CancellationToken ct = default)
        => await db.Cuestionarios.Where(c => c.IdCuestionario == idCuestionario)
            .ExecuteUpdateAsync(s => s.SetProperty(c => c.Estado, "Borrador"), ct);

    // PB-21: eliminación lógica del cuestionario (queda fuera de los análisis).
    public async Task EliminarAsync(int idCuestionario, CancellationToken ct = default)
        => await db.Cuestionarios.Where(c => c.IdCuestionario == idCuestionario)
            .ExecuteUpdateAsync(s => s.SetProperty(c => c.Estado, "Eliminado"), ct);

    // PB-21: al eliminar el cuestionario se borran también las observaciones asociadas
    // (las que referencian eventos de ese cuestionario) antes de declararlo Eliminado.
    public async Task EliminarObservacionesDeCuestionarioAsync(int idCuestionario, CancellationToken ct = default)
        => await db.Observaciones
            .Where(o => o.IdEvento.HasValue && db.Eventos.Any(e => e.IdEvento == o.IdEvento.Value && e.IdCuestionario == idCuestionario))
            .ExecuteDeleteAsync(ct);

    // PB-21: actualiza datos y preguntas/opciones en una transacción. Las preguntas
    // existentes se editan en sitio, las nuevas se insertan y las que ya no vienen se
    // marcan `eliminada` (no se borran si tienen respuestas por FK); sus opciones se
    // reconstruyen (nada las referencia).
    public async Task<int> ActualizarAsync(int idCuestionario, string titulo, string? descripcion, string estado, string? modoCalculo, int? umbralBajoMax, int? umbralMedioMax, List<(PreguntaRow Pregunta, List<OpcionPreguntaRow> Opciones)> preguntas, CancellationToken ct = default)
    {
        await using var tx = await db.Database.BeginTransactionAsync(ct);
        try
        {
            var fila = await db.Cuestionarios.FirstAsync(c => c.IdCuestionario == idCuestionario, ct);
            fila.Titulo = titulo;
            fila.Descripcion = descripcion;
            fila.Estado = estado;
            fila.ModoCalculo = modoCalculo;
            fila.UmbralBajoMax = umbralBajoMax;
            fila.UmbralMedioMax = umbralMedioMax;
            await db.SaveChangesAsync(ct);

            // Preguntas que ya no están en la edición -> baja lógica + se limpian sus opciones.
            var idsRecibidos = preguntas.Where(p => p.Pregunta.IdPregunta > 0).Select(p => p.Pregunta.IdPregunta).ToHashSet();
            var existentes = await db.Preguntas.Where(p => p.IdCuestionario == idCuestionario && !p.Eliminada).ToListAsync(ct);
            foreach (var vieja in existentes.Where(e => !idsRecibidos.Contains(e.IdPregunta)))
            {
                await db.OpcionesPregunta.Where(o => o.IdPregunta == vieja.IdPregunta).ExecuteDeleteAsync(ct);
                vieja.Eliminada = true;
            }
            await db.SaveChangesAsync(ct);

            var orden = 1;
            foreach (var (pregunta, opciones) in preguntas)
            {
                pregunta.Orden = orden++;
                if (pregunta.IdPregunta > 0)
                {
                    var existente = await db.Preguntas.FirstAsync(x => x.IdPregunta == pregunta.IdPregunta && x.IdCuestionario == idCuestionario, ct);
                    existente.Texto = pregunta.Texto;
                    existente.Orden = pregunta.Orden;
                    existente.Tipo = pregunta.Tipo;
                    existente.PuntajeMax = pregunta.PuntajeMax;
                    await db.OpcionesPregunta.Where(o => o.IdPregunta == pregunta.IdPregunta).ExecuteDeleteAsync(ct);
                    await db.SaveChangesAsync(ct);
                    foreach (var op in opciones)
                    {
                        op.IdPregunta = pregunta.IdPregunta;
                        db.OpcionesPregunta.Add(op);
                    }
                    await db.SaveChangesAsync(ct);
                }
                else
                {
                    pregunta.IdCuestionario = idCuestionario;
                    db.Preguntas.Add(pregunta);
                    await db.SaveChangesAsync(ct);
                    foreach (var op in opciones)
                    {
                        op.IdPregunta = pregunta.IdPregunta;
                        db.OpcionesPregunta.Add(op);
                    }
                    await db.SaveChangesAsync(ct);
                }
            }
            await tx.CommitAsync(ct);
            return idCuestionario;
        }
        catch
        {
            await tx.RollbackAsync(ct);
            throw;
        }
    }

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

    public async Task<ConversacionRow> CrearAsync(int idPaciente, int idPsicologo, int idSolicitante, CancellationToken ct = default)
    {
        var row = new ConversacionRow
        {
            IdPaciente = idPaciente,
            IdPsicologo = idPsicologo,
            IdSolicitante = idSolicitante,
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

    // Solicitud de bloqueo: pasa a BloqueoPendiente con motivo (mensajes congelados).
    public async Task SolicitarBloqueoAsync(int idConversacion, string motivo, CancellationToken ct = default)
    {
        var row = await db.Conversaciones
            .Where(c => c.IdConversacion == idConversacion)
            .FirstOrDefaultAsync(ct);
        if (row is null) return;
        row.Estado = "BloqueoPendiente";
        row.MotivoBloqueo = motivo;
        row.FechaRespuesta = DateTime.UtcNow;
        await db.SaveChangesAsync(ct);
    }

    // PB-31: buscador de pacientes (activos) con último riesgo, conteo de
    // cuestionarios respondidos (eventos tipo Cuestionario) y de psicólogos
    // autorizados. Filtros opcionales combinables (al menos uno lo valida el service).
    public async Task<List<PacienteBusquedaDto>> BuscarPacientesAsync(int idRolPaciente, int idEstadoActiva, string? q, string? nivel, int? minCuestionarios, bool? tienePsicologo, CancellationToken ct = default)
    {
        var idTipoCuestionario = await db.Set<TipoEventoRow>().AsNoTracking()
            .Where(t => t.Nombre == "Cuestionario").Select(t => t.IdTipoEvento)
            .FirstOrDefaultAsync(ct);
        var idAutorizado = await db.Estados.AsNoTracking()
            .Where(e => e.Nombre == "Autorizado").Select(e => e.IdEstado)
            .FirstOrDefaultAsync(ct);

        var pacientes = await db.Usuarios.AsNoTracking()
            .Where(u => u.IdRol == idRolPaciente && u.IdEstadoCuenta == idEstadoActiva
                && (string.IsNullOrWhiteSpace(q)
                    || u.Nombre.Contains(q) || u.Apellido.Contains(q) || u.Correo.Contains(q)))
            .OrderBy(u => u.Nombre).ThenBy(u => u.Apellido)
            .Take(50)
            .ToListAsync(ct);
        if (pacientes.Count == 0) return [];
        var ids = pacientes.Select(u => u.IdUsuario).ToList();

        var eventos = await db.Eventos.AsNoTracking()
            .Where(e => ids.Contains(e.IdPaciente) && e.IdTipoEvento == idTipoCuestionario)
            .ToListAsync(ct);
        var niveles = await db.NivelesRiesgo.AsNoTracking().ToListAsync(ct);
        var auts = await db.Set<AutorizacionRow>().AsNoTracking()
            .Where(a => ids.Contains(a.IdPaciente) && a.IdEstado == idAutorizado)
            .ToListAsync(ct);

        var lista = pacientes.Select(u =>
        {
            var evs = eventos.Where(e => e.IdPaciente == u.IdUsuario)
                .OrderByDescending(e => e.FechaEvento).ToList();
            var ultimo = evs.FirstOrDefault();
            var nivelNombre = ultimo?.IdNivelRiesgo is null
                ? null
                : niveles.FirstOrDefault(n => n.IdNivelRiesgo == ultimo.IdNivelRiesgo)?.Nombre;
            return new PacienteBusquedaDto(
                u.IdUsuario, $"{u.Nombre} {u.Apellido}".Trim(), u.Correo,
                ultimo?.PuntajeTotal, nivelNombre,
                ultimo?.FechaEvento, evs.Count,
                auts.Count(a => a.IdPaciente == u.IdUsuario));
        });

        if (!string.IsNullOrWhiteSpace(nivel))
            lista = lista.Where(r => string.Equals(r.UltimoNivel, nivel, StringComparison.OrdinalIgnoreCase));
        if (minCuestionarios is > 0)
            lista = lista.Where(r => r.CuestionariosRespondidos >= minCuestionarios.Value);
        if (tienePsicologo is not null)
            lista = lista.Where(r => tienePsicologo.Value ? r.Psicologos > 0 : r.Psicologos == 0);

        return lista.OrderBy(r => r.Nombre).Take(20).ToList();
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
        // Seguridad: nunca se exporta password_hash ni rutas de archivos.
        var usuarios = await db.Usuarios.AsNoTracking()
            .Select(u => new
            {
                u.IdUsuario, u.IdRol, u.IdEstadoCuenta, u.IdEstadoAprobacion,
                u.Nombre, u.Apellido, u.Correo, u.FechaNacimiento, u.FechaRegistro,
                u.IdGenero, u.UltimoAcceso
            }).ToListAsync(ct);
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
                usuarios
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
