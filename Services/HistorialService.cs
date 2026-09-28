using Mindzyxs_API.Dtos;
using Mindzyxs_API.Repositories;
using Mindzyxs_API.Repositories.Models;
using Mindzyxs_API.Utilities;

namespace Mindzyxs_API.Services;

public class HistorialService(
    IHistorialRepository historial,
    IUserRepository usuarios,
    ISesionRepository sesiones,
    IAuthLogRepository authLogs)
{
    // PB-13 HU1-PB13: lista del paciente (recientes primero), con filtro de fechas.
    // Vacía -> el front muestra "Aún no tienes registros emocionales".
    public async Task<List<HistorialItemDto>> ObtenerAsync(string? tokenRaw, DateTime? desde, DateTime? hasta, string? ip, CancellationToken ct = default)
    {
        var yo = await RequirePacienteAsync(tokenRaw, ct);
        var eventos = await historial.GetEventosAsync(yo.IdUsuario, desde, hasta, ct);
        if (eventos.Count == 0) return [];

        var niveles = await historial.GetNivelesAsync(ct);
        var cuestionarios = await historial.GetCuestionariosTituloAsync(
            eventos.Where(e => e.IdCuestionario.HasValue).Select(e => e.IdCuestionario!.Value).Distinct().ToList(), ct);

        await authLogs.LogAsync(yo.IdUsuario, yo.IdUsuario, LogAcciones.HistorialConsulta,
            $"Paciente:{yo.IdUsuario} consultó su historial emocional ({eventos.Count} registros)", ip, ct);

        return eventos.Select(e => ArmarItem(e, niveles, cuestionarios)).ToList();
    }

    // PB-13 HU1-PB13: detalle con nota propia, respuestas y apoyo del psicólogo.
    // Solo el dueño; si no -> "no encontrado" (404, sin revelar de quién es).
    public async Task<HistorialDetalleDto> ObtenerDetalleAsync(string? tokenRaw, int idEvento, string? ip, CancellationToken ct = default)
    {
        var yo = await RequirePacienteAsync(tokenRaw, ct);
        var e = await historial.FindEventoAsync(idEvento, ct);
        if (e is null || e.IdPaciente != yo.IdUsuario)
            throw new InvalidOperationException("Registro no encontrado.");

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
            detalle.Respuestas = respuestas.Select(r => new HistorialRespuestaDto
            {
                Pregunta = preguntas.TryGetValue(r.IdPregunta, out var t) ? t : $"Pregunta {r.IdPregunta}",
                Valor = r.Valor,
                Texto = r.Texto
            }).ToList();
        }

        var obs = await historial.GetObservacionesAsync(e.IdEvento, ct);
        detalle.Observaciones = obs.Select(o => new HistorialObservacionDto
        {
            Contenido = o.Contenido,
            Firma = o.Firma,
            Fecha = o.FechaObservacion
        }).ToList();

        await authLogs.LogAsync(yo.IdUsuario, yo.IdUsuario, LogAcciones.HistorialConsulta,
            $"Paciente:{yo.IdUsuario} vio el detalle del registro {e.IdEvento}", ip, ct);

        return detalle;
    }

    // PB-12 HU1-PB12: guarda el registro del estado emocional (escala 1-5 + nota opcional).
    // Valida que exista escala con el mensaje exacto de la HU, deriva el nivel con la
    // misma regla que el cuestionario (1-2 bajo, 3 medio, 4-5 alto), asigna fecha/hora
    // automática y registra REGISTRO_EMOCIONAL_CREADO en dbo.auth.
    public async Task<RegistroEmocionalCreadoResult> RegistrarEstadoEmocionalAsync(string? tokenRaw, RegistrarEstadoEmocionalVm vm, string? ip, CancellationToken ct = default)
    {
        var yo = await RequirePacienteAsync(tokenRaw, ct);
        if (vm.Puntaje is null or < 1 or > 5)
            throw new InvalidOperationException("Debes seleccionar un estado emocional para guardar");

        var (_, nivel) = CuestionarioService.CalcularPuntaje([vm.Puntaje.Value]);
        var idNivel = await historial.GetNivelIdAsync(nivel, ct);
        if (idNivel is null) throw new InvalidOperationException("Nivel de riesgo no configurado.");

        var idTipo = await historial.GetTipoEventoIdAsync("RegistroEmocional", ct);
        if (idTipo is null) throw new InvalidOperationException("Tipo de evento RegistroEmocional no configurado.");

        var nota = string.IsNullOrWhiteSpace(vm.Nota) ? null : vm.Nota.Trim();
        var fecha = DateTime.UtcNow;

        var idEvento = await historial.GuardarEstadoEmocionalAsync(yo.IdUsuario, idTipo.Value, idNivel.Value, vm.Puntaje.Value, nota, fecha, ct);

        await authLogs.LogAsync(yo.IdUsuario, yo.IdUsuario, LogAcciones.RegistroEmocionalCreado,
            $"Paciente:{yo.IdUsuario} registró su estado emocional ({vm.Puntaje}/5, nivel {nivel}){(nota is null ? "" : " con nota")} (evento {idEvento})", ip, ct);

        return new RegistroEmocionalCreadoResult(idEvento, fecha, vm.Puntaje.Value, nivel, nota);
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

    private async Task<UsuarioDto> RequirePacienteAsync(string? tokenRaw, CancellationToken ct)
    {
        if (string.IsNullOrWhiteSpace(tokenRaw))
            throw new UnauthorizedAccessException("Sesión requerida");
        var sesion = await sesiones.FindByTokenHashAsync(TokenHelper.Hash(tokenRaw.Trim()), ct);
        if (sesion is null || sesion.ExpiraEn < DateTime.UtcNow)
            throw new UnauthorizedAccessException("Sesión requerida");
        var user = await usuarios.FindByIdAsync(sesion.IdUsuario, ct)
            ?? throw new UnauthorizedAccessException("Sesión requerida");
        return user;
    }
}
