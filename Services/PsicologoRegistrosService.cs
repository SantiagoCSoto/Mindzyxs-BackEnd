using Mindzyxs_API.Dtos;
using Mindzyxs_API.Repositories;
using Mindzyxs_API.Repositories.Models;
using Mindzyxs_API.Utilities;

namespace Mindzyxs_API.Services;

public class PsicologoRegistrosService(
    IHistorialRepository historial,
    IUserRepository users,
    ICatalogRepository catalogs,
    ISesionRepository sesiones,
    IAuthLogRepository authLogs)
{
    // PB-18 HU1-PB18: pacientes autorizados con búsqueda (nombre, id, estado).
    public async Task<List<ProgresoPacienteDto>> PacientesAsync(string? tokenRaw, string? q, string? ip, CancellationToken ct = default)
    {
        var yo = await RequirePsicologoAsync(tokenRaw, ct);
        var idAut = await historial.GetEstadoIdAsync("Autorizado", ct) ?? -1;
        var lista = await historial.GetPacientesAutorizadosAsync(yo.IdUsuario, idAut, ct);
        if (!string.IsNullOrWhiteSpace(q))
        {
            var t = q.Trim().ToLower();
            lista = lista.Where(u => ($"{u.Nombre} {u.Apellido}".ToLower().Contains(t)
                || u.Correo.ToLower().Contains(t))).ToList();
        }

        await authLogs.LogAsync(yo.IdUsuario, yo.IdUsuario, LogAcciones.RegistrosConsulta,
            $"Psicólogo:{yo.IdUsuario} consultó sus pacientes autorizados ({lista.Count})", ip, ct);

        return lista.Select(u => new ProgresoPacienteDto
        {
            Id = u.IdUsuario,
            Nombre = $"{u.Nombre} {u.Apellido}".Trim()
        }).ToList();
    }

    // PB-18 HU1-PB18: registros del paciente (emocionales + cuestionario, fecha desc).
    public async Task<List<HistorialItemDto>> RegistrosAsync(string? tokenRaw, int idPaciente, DateTime? desde, DateTime? hasta, string? ip, CancellationToken ct = default)
    {
        var yo = await RequirePsicologoAsync(tokenRaw, ct);
        if (desde.HasValue && hasta.HasValue && desde.Value.Date > hasta.Value.Date)
            throw new InvalidOperationException("La fecha \"Desde\" no puede ser posterior a \"Hasta\".");
        var idAut = await historial.GetEstadoIdAsync("Autorizado", ct) ?? -1;
        if (!await historial.EstaAutorizadoAsync(idPaciente, yo.IdUsuario, idAut, ct))
            throw new InvalidOperationException("No tienes autorización para ver los registros de este paciente.");

        var eventos = await historial.GetEventosAsync(idPaciente, desde, hasta, ct);
        if (eventos.Count == 0) return [];

        var niveles = await historial.GetNivelesAsync(ct);
        var cuestionarios = await historial.GetCuestionariosTituloAsync(
            eventos.Where(e => e.IdCuestionario.HasValue).Select(e => e.IdCuestionario!.Value).Distinct().ToList(), ct);

        await authLogs.LogAsync(yo.IdUsuario, idPaciente, LogAcciones.RegistrosConsulta,
            $"Psicólogo:{yo.IdUsuario} consultó registros del paciente:{idPaciente} ({eventos.Count})", ip, ct);

        return eventos.Select(e => ArmarItem(e, niveles, cuestionarios)).ToList();
    }

    // PB-18 HU1-PB18: detalle completo (fecha/hora, estado, notas, resultado resumido).
    public async Task<HistorialDetalleDto> DetalleAsync(string? tokenRaw, int idEvento, string? ip, CancellationToken ct = default)
    {
        var yo = await RequirePsicologoAsync(tokenRaw, ct);
        var e = await historial.FindEventoAsync(idEvento, ct);
        if (e is null)
            throw new InvalidOperationException("Registro no encontrado.");
        var idAut = await historial.GetEstadoIdAsync("Autorizado", ct) ?? -1;
        if (!await historial.EstaAutorizadoAsync(e.IdPaciente, yo.IdUsuario, idAut, ct))
            throw new InvalidOperationException("No tienes autorización para ver este registro.");

        var niveles = await historial.GetNivelesAsync(ct);
        var cuestionarios = await historial.GetCuestionariosTituloAsync(
            e.IdCuestionario.HasValue ? [e.IdCuestionario.Value] : [], ct);
        var item = ArmarItem(e, niveles, cuestionarios);

        var detalle = new HistorialDetalleDto
        {
            Id = item.Id,
            Fecha = item.Fecha,
            Tipo = item.Tipo,
            Puntaje = item.Puntaje,
            Nivel = item.Nivel,
            Cuestionario = item.Cuestionario,
            Resumen = item.Resumen,
            Nota = e.Nota
        };

        var respuestas = await historial.GetRespuestasAsync(e.IdEvento, ct);
        if (respuestas.Count > 0)
        {
            var preguntas = await historial.GetPreguntasTextoAsync(respuestas.Select(r => r.IdPregunta).Distinct().ToList(), ct);
            var textosOpcion = await historial.GetTextosOpcionAsync(respuestas.Select(r => r.IdPregunta).Distinct().ToList(), ct);
            detalle.Respuestas = respuestas.Select(r => new HistorialRespuestaDto
            {
                Pregunta = preguntas.TryGetValue(r.IdPregunta, out var t) ? t : $"Pregunta {r.IdPregunta}",
                Valor = r.Valor,
                Texto = r.Texto ?? (r.Valor.HasValue && textosOpcion.TryGetValue((r.IdPregunta, r.Valor.Value), out var to) ? to : null)
            }).ToList();
        }

        var obs = await historial.GetObservacionesAsync(e.IdEvento, ct);
        detalle.Observaciones = obs.Select(o => new HistorialObservacionDto
        {
            Contenido = o.Contenido,
            Firma = o.Firma,
            Fecha = o.FechaObservacion
        }).ToList();

        await authLogs.LogAsync(yo.IdUsuario, e.IdPaciente, LogAcciones.RegistroDetalle,
            $"Psicólogo:{yo.IdUsuario} vio el detalle del registro {e.IdEvento} del paciente:{e.IdPaciente}", ip, ct);

        return detalle;
    }

    private static HistorialItemDto ArmarItem(
        EventoRow e, Dictionary<int, string> niveles, Dictionary<int, string> cuestionarios)
    {
        var esCuest = e.IdTipoEvento == 2;
        var nivel = e.IdNivelRiesgo.HasValue && niveles.TryGetValue(e.IdNivelRiesgo.Value, out var n) ? n : null;
        var cuest = e.IdCuestionario.HasValue && cuestionarios.TryGetValue(e.IdCuestionario.Value, out var t) ? t : null;
        var resumen = esCuest
            ? $"Cuestionario{(cuest is null ? "" : $" \"{cuest}\"")}{(e.PuntajeTotal is null ? "" : $": puntaje {e.PuntajeTotal}")}{(nivel is null ? "" : $", riesgo {nivel}")}"
            : $"Registro emocional{(e.PuntajeTotal is null ? "" : $": {e.PuntajeTotal}/5")}";
        return new HistorialItemDto
        {
            Id = e.IdEvento,
            Fecha = e.FechaEvento,
            Tipo = esCuest ? "Cuestionario" : "RegistroEmocional",
            Puntaje = e.PuntajeTotal,
            Nivel = nivel,
            Cuestionario = cuest,
            Resumen = resumen
        };
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
