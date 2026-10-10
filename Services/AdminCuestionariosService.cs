using Mindzyxs_API.Dtos;
using Mindzyxs_API.Repositories;
using Mindzyxs_API.Repositories.Models;
using Mindzyxs_API.Utilities;

namespace Mindzyxs_API.Services;

public class AdminCuestionariosService(
    ICuestionarioRepository cuestionarios,
    IUserRepository users,
    ICatalogRepository catalogs,
    ISesionRepository sesiones,
    IAuthLogRepository authLogs,
    MindzyxsDbContext db)
{
    // PB-28 HU1-PB28: lista cuestionarios Predeterminado del admin (con búsqueda).
    // Solo administrador activo.
    public async Task<List<CuestionarioPredeterminadoListaDto>> ListarAsync(string? tokenRaw, string? q, CancellationToken ct = default)
    {
        var adminId = await RequireAdminAsync(tokenRaw, ct);
        var busqueda = string.IsNullOrWhiteSpace(q) ? null : q.Trim();
        var lista = await cuestionarios.GetPredeterminadosAsync(busqueda, ct);
        return lista.Select(c => new CuestionarioPredeterminadoListaDto
        {
            Id = c.IdCuestionario,
            Titulo = c.Titulo,
            Descripcion = c.Descripcion,
            Estado = c.Estado,
            NumPreguntas = db.Preguntas.Count(p => p.IdCuestionario == c.IdCuestionario && !p.Eliminada),
            FechaCreacion = c.FechaCreacion
        }).ToList();
    }

    // PB-28: detalle de un cuestionario predeterminado para editar.
    public async Task<CuestionarioPredeterminadoDetalleDto> ObtenerDetalleAsync(string? tokenRaw, int id, CancellationToken ct = default)
    {
        var adminId = await RequireAdminAsync(tokenRaw, ct);
        var c = await cuestionarios.FindPredeterminadoAsync(id, ct)
            ?? throw new InvalidOperationException("Cuestionario predeterminado no encontrado.");
        var (preguntas, opciones) = await cuestionarios.GetCompletoAsync(id, ct);
        return new CuestionarioPredeterminadoDetalleDto
        {
            Id = c.IdCuestionario,
            Titulo = c.Titulo,
            Descripcion = c.Descripcion,
            Estado = c.Estado,
            ModoCalculo = c.ModoCalculo,
            UmbralBajoMax = c.UmbralBajoMax,
            UmbralMedioMax = c.UmbralMedioMax,
            FechaCreacion = c.FechaCreacion,
            Preguntas = preguntas
                .OrderBy(p => p.Orden)
                .Select(p => new PreguntaPsicologoDto
                {
                    Id = p.IdPregunta,
                    Texto = p.Texto,
                    Orden = p.Orden,
                    Tipo = p.Tipo,
                    Opciones = opciones.Where(o => o.IdPregunta == p.IdPregunta).OrderBy(o => o.Orden)
                        .Select(o => new OpcionPsicologoDto { Id = o.IdOpcion, Texto = o.Texto, Valor = o.Valor, Orden = o.Orden })
                        .ToList()
                })
                .ToList()
        };
    }

    // PB-28 HU1-PB28: crea un cuestionario Predeterminado (valida campos obligatorios).
    // Guarda log CUENTIONARIO_PREDETERMINADO_CREADO.
    public async Task<CuestionarioPredeterminadoResultadoDto> CrearAsync(string? tokenRaw, CuestionarioPredeterminadoCrearDto dto, string? ip, CancellationToken ct = default)
    {
        var adminId = await RequireAdminAsync(tokenRaw, ct);

        if (string.IsNullOrWhiteSpace(dto.Titulo))
            throw new InvalidOperationException("Debes completar todos los campos obligatorios");
        if (dto.Preguntas is null || dto.Preguntas.Count == 0)
            throw new InvalidOperationException("Debes completar todos los campos obligatorios");

        var preguntas = ValidarPreguntas(dto.Preguntas);
        var puntajeMax = preguntas.Sum(x => x.Item2.Max(o => o.Valor));
        var (modo, bajoMax, medioMax) = ValidarUmbrales(dto.Umbrales, puntajeMax);

        var fila = new CuestionarioRow
        {
            Titulo = dto.Titulo.Trim(),
            Descripcion = string.IsNullOrWhiteSpace(dto.Descripcion) ? null : dto.Descripcion.Trim(),
            Tipo = "Predeterminado",
            Estado = dto.Publicar ? "Publicado" : "Inactivo",
            IdCreador = adminId ?? 0,
            ModoCalculo = modo,
            UmbralBajoMax = bajoMax,
            UmbralMedioMax = medioMax
        };
        var id = await cuestionarios.CrearPredeterminadoAsync(fila, preguntas, ct);

        await authLogs.LogAsync(adminId, adminId, LogAcciones.CuestionarioPredeterminadoCreado,
            $"Admin creó el cuestionario predeterminado {id} \"{fila.Titulo}\" ({fila.Estado}" +
            (modo == "suma" ? $", intervalos Bajo≤{bajoMax} Medio≤{medioMax}" : ", regla de riesgo actual") + ")", ip, ct);

        return new CuestionarioPredeterminadoResultadoDto { Id = id, Mensaje = "Cuestionario predeterminado guardado correctamente" };
    }

    // PB-28: edita un cuestionario Predeterminado existente.
    // Valida campos obligatorios y log CUENTIONARIO_PREDETERMINADO_EDITADO.
    public async Task<CuestionarioPredeterminadoResultadoDto> ActualizarAsync(string? tokenRaw, int id, CuestionarioPredeterminadoCrearDto dto, string? ip, CancellationToken ct = default)
    {
        var adminId = await RequireAdminAsync(tokenRaw, ct);
        var c = await cuestionarios.FindPredeterminadoAsync(id, ct)
            ?? throw new InvalidOperationException("Cuestionario predeterminado no encontrado.");

        if (string.IsNullOrWhiteSpace(dto.Titulo))
            throw new InvalidOperationException("Debes completar todos los campos obligatorios");
        if (dto.Preguntas is null || dto.Preguntas.Count == 0)
            throw new InvalidOperationException("Debes completar todos los campos obligatorios");

        var preguntas = ValidarPreguntas(dto.Preguntas);
        var puntajeMax = preguntas.Sum(x => x.Item2.Max(o => o.Valor));
        var (modo, bajoMax, medioMax) = ValidarUmbrales(dto.Umbrales, puntajeMax);

        var estado = dto.Publicar ? "Publicado" : "Inactivo";
        var titulo = dto.Titulo.Trim();
        await cuestionarios.ActualizarPredeterminadoAsync(id, titulo,
            string.IsNullOrWhiteSpace(dto.Descripcion) ? null : dto.Descripcion.Trim(),
            estado, modo, bajoMax, medioMax, preguntas, ct);

        await authLogs.LogAsync(adminId, adminId, LogAcciones.CuestionarioPredeterminadoEditado,
            $"Admin editó el cuestionario predeterminado {id} \"{titulo}\" ({estado}" +
            (modo == "suma" ? $", intervalos Bajo≤{bajoMax} Medio≤{medioMax})" : ")") + "", ip, ct);

        return new CuestionarioPredeterminadoResultadoDto { Id = id, Mensaje = "Cuestionario predeterminado guardado correctamente" };
    }

    // PB-28: desactiva un cuestionario predeterminado (estado -> Inactivo).
    // Log CUENTIONARIO_PREDETERMINADO_DESACTIVADO.
    public async Task<string> DesactivarAsync(string? tokenRaw, int id, string? ip, CancellationToken ct = default)
    {
        var adminId = await RequireAdminAsync(tokenRaw, ct);
        var c = await cuestionarios.FindPredeterminadoAsync(id, ct)
            ?? throw new InvalidOperationException("Cuestionario predeterminado no encontrado.");

        await cuestionarios.DesactivarAsync(id, ct);
        await authLogs.LogAsync(adminId, adminId, LogAcciones.CuestionarioPredeterminadoDesactivado,
            $"Admin desactivó el cuestionario predeterminado {id} \"{c.Titulo}\"", ip, ct);
        return "Cuestionario predeterminado desactivado correctamente";
    }

    // Valida título, preguntas (texto, min 2 opciones, valores >=0).
    private static List<(PreguntaRow Pregunta, List<OpcionPreguntaRow> Opciones)> ValidarPreguntas(List<PreguntaCrearDto> pregs)
    {
        var resultado = new List<(PreguntaRow, List<OpcionPreguntaRow>)>();
        var ordenP = 1;
        foreach (var p in pregs)
        {
            if (string.IsNullOrWhiteSpace(p.Texto))
                throw new InvalidOperationException("Debes completar todos los campos obligatorios");
            if (p.Opciones is null || p.Opciones.Count < 2)
                throw new InvalidOperationException("Debes completar todos los campos obligatorios");
            var opciones = new List<OpcionPreguntaRow>();
            var ordenO = 1;
            foreach (var o in p.Opciones)
            {
                if (string.IsNullOrWhiteSpace(o.Texto))
                    throw new InvalidOperationException("Debes completar todos los campos obligatorios");
                if (o.Valor < 0)
                    throw new InvalidOperationException("Debes completar todos los campos obligatorios");
                opciones.Add(new OpcionPreguntaRow { Texto = o.Texto.Trim(), Valor = o.Valor, Orden = ordenO++ });
            }
            var tipo = string.IsNullOrWhiteSpace(p.Tipo) ? "opcion_multiple" : p.Tipo.Trim();
            resultado.Add((new PreguntaRow
            {
                IdPregunta = p.Id ?? 0,
                Texto = p.Texto.Trim(),
                Orden = ordenP++,
                Tipo = tipo,
                PuntajeMax = opciones.Max(o => o.Valor),
                Eliminada = false
            }, opciones));
        }
        return resultado;
    }

    // Valida umbrales: bajoMax < medioMax < puntajeMax.
    private static (string? Modo, int? BajoMax, int? MedioMax) ValidarUmbrales(UmbralesRiesgoDto? umbrales, int puntajeMax)
    {
        if (umbrales is null) return (null, null, null);
        if (umbrales.BajoMax < 0 || umbrales.MedioMax <= umbrales.BajoMax)
            throw new InvalidOperationException("Debes completar todos los campos obligatorios");
        if (umbrales.MedioMax >= puntajeMax)
            throw new InvalidOperationException("Debes completar todos los campos obligatorios");
        return ("suma", umbrales.BajoMax, umbrales.MedioMax);
    }

    // Requiere administrador con sesión activa.
    private async Task<int?> RequireAdminAsync(string? tokenRaw, CancellationToken ct)
    {
        if (string.IsNullOrWhiteSpace(tokenRaw))
            throw new UnauthorizedAccessException("Tu sesión ha expirado. Inicia sesión nuevamente");
        var sesion = await sesiones.FindByTokenHashAsync(TokenHelper.Hash(tokenRaw.Trim()), ct);
        if (sesion is null || sesion.ExpiraEn < DateTime.UtcNow)
            throw new UnauthorizedAccessException("Tu sesión ha expirado. Inicia sesión nuevamente");
        var user = await users.FindByIdAsync(sesion.IdUsuario, ct)
            ?? throw new UnauthorizedAccessException("Tu sesión ha expirado. Inicia sesión nuevamente");
        var rol = await users.GetRolNombreAsync(user.IdRol, ct);
        if (!string.Equals(rol, AppRoles.Administrador, StringComparison.OrdinalIgnoreCase))
            throw new InvalidOperationException("No tienes permisos para acceder a esta función");
        return user.IdUsuario;
    }
}