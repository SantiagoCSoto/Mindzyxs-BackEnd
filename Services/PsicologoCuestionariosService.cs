using Mindzyxs_API.Dtos;
using Mindzyxs_API.Repositories;
using Mindzyxs_API.Repositories.Models;
using Mindzyxs_API.Utilities;

namespace Mindzyxs_API.Services;

public class PsicologoCuestionariosService(
    ICuestionarioRepository cuestionarios,
    IUserRepository users,
    ICatalogRepository catalogs,
    ISesionRepository sesiones,
    IAuthLogRepository authLogs)
{
    // PB-20 HU1-PB20: crea el cuestionario Personalizado (Borrador o Publicado)
    // con preguntas, opciones con valor e intervalos de riesgo configurables.
    public async Task<CuestionarioCreadoDto> CrearAsync(string? tokenRaw, CuestionarioCrearDto dto, string? ip, CancellationToken ct = default)
    {
        var yo = await RequirePsicologoAsync(tokenRaw, ct);

        if (string.IsNullOrWhiteSpace(dto.Titulo))
            throw new InvalidOperationException("El título es obligatorio.");
        if (dto.Preguntas is null || dto.Preguntas.Count == 0)
            throw new InvalidOperationException("Debes agregar al menos una pregunta.");

        var preguntas = ValidarPreguntas(dto.Preguntas);
        var puntajeMax = preguntas.Sum(x => x.Item2.Max(o => o.Valor));
        var (modo, bajoMax, medioMax) = ValidarUmbrales(dto.Umbrales, puntajeMax);

        var fila = new CuestionarioRow
        {
            Titulo = dto.Titulo.Trim(),
            Descripcion = string.IsNullOrWhiteSpace(dto.Descripcion) ? null : dto.Descripcion.Trim(),
            Tipo = "Personalizado",
            Estado = dto.Publicar ? "Publicado" : "Borrador",
            IdCreador = yo.IdUsuario,
            ModoCalculo = modo,
            UmbralBajoMax = bajoMax,
            UmbralMedioMax = medioMax
        };
        var id = await cuestionarios.CrearAsync(fila, preguntas, ct);

        await authLogs.LogAsync(yo.IdUsuario, yo.IdUsuario, LogAcciones.CuestionarioCreado,
            $"Psicólogo:{yo.IdUsuario} creó el cuestionario {id} \"{fila.Titulo}\" ({fila.Estado}" +
            (modo == "suma" ? $", intervalos Bajo≤{bajoMax} Medio≤{medioMax}" : ", regla de riesgo actual") + ")", ip, ct);

        return new CuestionarioCreadoDto { Id = id, Mensaje = "Cuestionario guardado correctamente" };
    }

    // PB-21 HU1-PB21: lista "Mis cuestionarios" (Borrador/Publicado) con búsqueda en orden.
    public async Task<List<CuestionarioPsicologoListaDto>> ListarAsync(string? tokenRaw, string? q, CancellationToken ct = default)
    {
        var yo = await RequirePsicologoAsync(tokenRaw, ct);
        var busqueda = string.IsNullOrWhiteSpace(q) ? null : q.Trim();
        return await cuestionarios.GetDePsicologoAsync(yo.IdUsuario, busqueda, ct);
    }

    // PB-21: detalle del cuestionario propio (título, descripción, estado, umbrales,
    // preguntas y opciones con sus ids para prellenar la edición).
    public async Task<CuestionarioPsicologoDetalleDto> ObtenerDetalleAsync(string? tokenRaw, int id, CancellationToken ct = default)
    {
        var yo = await RequirePsicologoAsync(tokenRaw, ct);
        var c = await cuestionarios.FindDePsicologoAsync(id, yo.IdUsuario, ct)
            ?? throw new InvalidOperationException("Cuestionario no encontrado.");
        var (preguntas, opciones) = await cuestionarios.GetCompletoAsync(id, ct);
        return new CuestionarioPsicologoDetalleDto
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

    // PB-21: confirmación de editar un cuestionario Publicado -> pasa a Borrador (log).
    public async Task<string> DespublicarAsync(string? tokenRaw, int id, string? ip, CancellationToken ct = default)
    {
        var yo = await RequirePsicologoAsync(tokenRaw, ct);
        var c = await cuestionarios.FindDePsicologoAsync(id, yo.IdUsuario, ct)
            ?? throw new InvalidOperationException("Cuestionario no encontrado.");
        if (!string.Equals(c.Estado, "Publicado", StringComparison.OrdinalIgnoreCase))
            throw new InvalidOperationException("El cuestionario no está publicado.");
        await cuestionarios.DespublicarAsync(id, ct);
        await authLogs.LogAsync(yo.IdUsuario, yo.IdUsuario, LogAcciones.CuestionarioEditado,
            $"Psicólogo:{yo.IdUsuario} pasó el cuestionario {id} \"{c.Titulo}\" a Borrador (confirmación para editarlo)", ip, ct);
        return "El cuestionario ahora es un borrador";
    }

    // PB-21: guarda la edición del cuestionario (Borrador o Publicado) con sus preguntas
    // y opciones. Un publicado primero debe pasar a Borrador (flujo con confirmación).
    public async Task<CuestionarioCreadoDto> ActualizarAsync(string? tokenRaw, int id, CuestionarioCrearDto dto, string? ip, CancellationToken ct = default)
    {
        var yo = await RequirePsicologoAsync(tokenRaw, ct);
        var anterior = await cuestionarios.FindDePsicologoAsync(id, yo.IdUsuario, ct)
            ?? throw new InvalidOperationException("Cuestionario no encontrado.");
        if (string.Equals(anterior.Estado, "Publicado", StringComparison.OrdinalIgnoreCase))
            throw new InvalidOperationException("Para modificar un cuestionario publicado pásalo primero a borrador.");
        if (string.IsNullOrWhiteSpace(dto.Titulo))
            throw new InvalidOperationException("El título es obligatorio.");
        if (dto.Preguntas is null || dto.Preguntas.Count == 0)
            throw new InvalidOperationException("El cuestionario debe tener al menos una pregunta");

        var preguntas = ValidarPreguntas(dto.Preguntas);
        var puntajeMax = preguntas.Sum(x => x.Item2.Max(o => o.Valor));
        var (modo, bajoMax, medioMax) = ValidarUmbrales(dto.Umbrales, puntajeMax);

        var estado = dto.Publicar ? "Publicado" : "Borrador";
        var titulo = dto.Titulo.Trim();
        await cuestionarios.ActualizarAsync(id, titulo,
            string.IsNullOrWhiteSpace(dto.Descripcion) ? null : dto.Descripcion.Trim(),
            estado, modo, bajoMax, medioMax, preguntas, ct);

        await authLogs.LogAsync(yo.IdUsuario, yo.IdUsuario, LogAcciones.CuestionarioEditado,
            $"Psicólogo:{yo.IdUsuario} editó el cuestionario {id} \"{titulo}\" ({estado}" +
            (modo == "suma" ? $", intervalos Bajo≤{bajoMax} Medio≤{medioMax})" : ")") + "", ip, ct);

        return new CuestionarioCreadoDto { Id = id, Mensaje = "Cuestionario actualizado correctamente" };
    }

    // PB-21: elimina el cuestionario con confirmación extra: baja lógica (Eliminado,
    // fuera de los análisis) + borrado de las observaciones asociadas + log.
    public async Task<string> EliminarAsync(string? tokenRaw, int id, bool confirmar, string? ip, CancellationToken ct = default)
    {
        var yo = await RequirePsicologoAsync(tokenRaw, ct);
        var c = await cuestionarios.FindDePsicologoAsync(id, yo.IdUsuario, ct)
            ?? throw new InvalidOperationException("Cuestionario no encontrado.");
        if (!confirmar)
            throw new InvalidOperationException("Debes confirmar la eliminación del cuestionario.");

        await cuestionarios.EliminarObservacionesDeCuestionarioAsync(id, ct);
        await cuestionarios.EliminarAsync(id, ct);
        await authLogs.LogAsync(yo.IdUsuario, yo.IdUsuario, LogAcciones.CuestionarioEliminado,
            $"Psicólogo:{yo.IdUsuario} eliminó el cuestionario {id} \"{c.Titulo}\" (pasa a Eliminado, no se usa en los análisis)", ip, ct);
        return "Cuestionario eliminado correctamente";
    }

    // Valida título no, preguntas: texto, mínimo 2 opciones y valores >= 0 (PB-20/21).
    // Devuelve las filas listas para insertar (IdPregunta > 0 si viene del editor).
    private static List<(PreguntaRow Pregunta, List<OpcionPreguntaRow> Opciones)> ValidarPreguntas(List<PreguntaCrearDto> pregs)
    {
        var resultado = new List<(PreguntaRow, List<OpcionPreguntaRow>)>();
        var ordenP = 1;
        foreach (var p in pregs)
        {
            if (string.IsNullOrWhiteSpace(p.Texto))
                throw new InvalidOperationException("Cada pregunta debe tener texto.");
            if (p.Opciones is null || p.Opciones.Count < 2)
                throw new InvalidOperationException("Cada pregunta debe tener al menos dos opciones.");
            var opciones = new List<OpcionPreguntaRow>();
            var ordenO = 1;
            foreach (var o in p.Opciones)
            {
                if (string.IsNullOrWhiteSpace(o.Texto))
                    throw new InvalidOperationException("Cada opción debe tener texto.");
                if (o.Valor < 0)
                    throw new InvalidOperationException("El valor de cada opción debe ser mayor o igual a cero.");
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

    // Valida los intervalos de riesgo configurables (PB-20/21): por defecto regla actual.
    private static (string? Modo, int? BajoMax, int? MedioMax) ValidarUmbrales(UmbralesRiesgoDto? umbrales, int puntajeMax)
    {
        if (umbrales is null) return (null, null, null);
        if (umbrales.BajoMax < 0 || umbrales.MedioMax <= umbrales.BajoMax)
            throw new InvalidOperationException("Los intervalos de riesgo no son válidos: el límite de Bajo debe ser menor que el de Medio.");
        if (umbrales.MedioMax >= puntajeMax)
            throw new InvalidOperationException("Los intervalos de riesgo no son válidos: el límite de Medio debe ser menor que el puntaje máximo posible.");
        return ("suma", umbrales.BajoMax, umbrales.MedioMax);
    }

    // Requiere psicólogo con cuenta activa y aprobada.
    private async Task<UsuarioDto> RequirePsicologoAsync(string? tokenRaw, CancellationToken ct)
    {
        if (string.IsNullOrWhiteSpace(tokenRaw))
            throw new UnauthorizedAccessException("Sesión requerida");
        var sesion = await sesiones.FindByTokenHashAsync(TokenHelper.Hash(tokenRaw.Trim()), ct);
        if (sesion is null || sesion.ExpiraEn < DateTime.UtcNow)
            throw new UnauthorizedAccessException("Sesión requerida");
        var user = await users.FindByIdAsync(sesion.IdUsuario, ct)
            ?? throw new UnauthorizedAccessException("Sesión requerida");
        var rol = await users.GetRolNombreAsync(user.IdRol, ct);
        if (!string.Equals(rol, AppRoles.Psicologo, StringComparison.OrdinalIgnoreCase))
            throw new InvalidOperationException("No tienes permisos para acceder a esta función");
        var estado = await users.GetEstadoCuentaNombreAsync(user.IdEstadoCuenta, ct);
        if (!string.Equals(estado, AccountStates.Activa, StringComparison.OrdinalIgnoreCase))
            throw new InvalidOperationException("La cuenta no está activa.");
        if (user.IdEstadoAprobacion.HasValue)
        {
            var aprob = await catalogs.GetEstadoNombreAsync(user.IdEstadoAprobacion.Value, ct);
            if (!string.Equals(aprob, AccountStates.Aprobado, StringComparison.OrdinalIgnoreCase))
                throw new InvalidOperationException("El psicólogo aún no está aprobado.");
        }
        return user;
    }
}
