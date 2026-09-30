using Mindzyxs_API.Dtos;
using Mindzyxs_API.Repositories;
using Mindzyxs_API.Repositories.Models;
using Mindzyxs_API.Utilities;

namespace Mindzyxs_API.Services;

public class CuestionarioService(
    ICuestionarioRepository cuestionarios,
    IHistorialRepository historial,
    ISesionRepository sesiones,
    IUserRepository usuarios,
    IAuthLogRepository authLogs)
{
    // PB-07 HU1-PB7: siguiente cuestionario pendiente (rotación).
    // Invitado ve Predeterminado+Publicado excepto los ya hechos (excluidos = cookie cuest_*).
    // Sirve N preguntas con M opciones, todo dinámico desde BD.
    public async Task<CuestionarioInvitadoDto> ObtenerPendienteAsync(HashSet<int> excluidos, CancellationToken ct = default)
    {
        var publicados = await cuestionarios.GetPublicadosInvitadoAsync(ct);
        var actual = publicados.FirstOrDefault(c => !excluidos.Contains(c.IdCuestionario));
        if (actual is null)
            return new CuestionarioInvitadoDto
            {
                Disponible = false,
                Mensaje = "No tienes cuestionarios pendientes por responder."
            };

        // Corrección BD: preguntas+opciones en 1 round-trip (sp_cuestionario_completo).
        var (preguntas, opciones) = await cuestionarios.GetCompletoAsync(actual.IdCuestionario, ct);

        return new CuestionarioInvitadoDto
        {
            Disponible = true,
            Id = actual.IdCuestionario,
            Titulo = actual.Titulo,
            Descripcion = actual.Descripcion,
            AvisoObligatorio = true,
            Preguntas = preguntas.Select(p => new PreguntaInvitadoDto
            {
                Id = p.IdPregunta,
                Texto = p.Texto,
                Orden = p.Orden,
                Opciones = opciones.Where(o => o.IdPregunta == p.IdPregunta)
                    .Select(o => new OpcionInvitadoDto { Id = o.IdOpcion, Texto = o.Texto, Valor = o.Valor, Orden = o.Orden })
                    .ToList()
            }).ToList()
        };
    }

    // PB-07 HU1-PB7: registra el inicio del cuestionario en dbo.auth.
    public async Task RegistrarInicioAsync(string? tokenRaw, int cuestionarioId, string? ip, CancellationToken ct = default)
    {
        var actorId = await ResolveActorIdAsync(tokenRaw, ct);
        var c = await cuestionarios.FindPublicadoInvitadoAsync(cuestionarioId, ct);
        var detalle = c is null
            ? $"{RolOrInvitado(actorId)} inició un cuestionario no disponible (id: {cuestionarioId})"
            : $"{RolOrInvitado(actorId)} inició el cuestionario \"{c.Titulo}\" (id: {c.IdCuestionario})";
        await authLogs.LogAsync(actorId, actorId, LogAcciones.CuestionarioInvitadoInicio, detalle, ip, ct);
    }

    // PB-07 HU1-PB7: valida el 100% (todas obligatorias), calcula puntaje/nivel y registra el fin.
    // No persiste respuestas: respuestas→evento→paciente NOT NULL, el invitado no tiene cuenta.
    // El front guarda las respuestas en su sesión (cookies) y las reusa en PB-08.
    // Lanza InvalidOperationException con el mensaje exacto para el 400.
    public async Task<FinalizarCuestionarioResult> FinalizarAsync(string? tokenRaw, FinalizarCuestionarioVm vm, string? ip, CancellationToken ct = default)
    {
        var actorId = await ResolveActorIdAsync(tokenRaw, ct);
        var c = await cuestionarios.FindPublicadoInvitadoAsync(vm.CuestionarioId, ct);
        if (c is null) throw new InvalidOperationException("Cuestionario no disponible.");

        // Corrección BD: preguntas+opciones en 1 round-trip (sp_cuestionario_completo).
        var (preguntas, opciones) = await cuestionarios.GetCompletoAsync(c.IdCuestionario, ct);
        var porPregunta = vm.Respuestas.GroupBy(r => r.PreguntaId).ToDictionary(g => g.Key, g => g.First().OpcionId);

        var valoresEscala = new List<int>();
        foreach (var p in preguntas)
        {
            if (!porPregunta.TryGetValue(p.IdPregunta, out var opcionId) || opcionId <= 0)
                throw new InvalidOperationException("Debes seleccionar una respuesta para continuar");
            var op = opciones.FirstOrDefault(o => o.IdOpcion == opcionId && o.IdPregunta == p.IdPregunta);
            if (op is null) throw new InvalidOperationException("La respuesta seleccionada no es válida");
            // Escala: tipo 'escala' u 'opcion_multiple' con valores numéricos
            // (el cuestionario 1 usa opcion_multiple). 'binaria' no entra.
            if (string.Equals(p.Tipo, "escala", StringComparison.OrdinalIgnoreCase)
                || string.Equals(p.Tipo, "opcion_multiple", StringComparison.OrdinalIgnoreCase))
                valoresEscala.Add(op.Valor);
        }

        var (puntaje, nivel) = CalcularPuntaje(valoresEscala);
        var detalle = $"{RolOrInvitado(actorId)} completó el cuestionario \"{c.Titulo}\" (id: {c.IdCuestionario}) al 100%: puntaje {puntaje}, nivel {nivel}";
        await authLogs.LogAsync(actorId, actorId, LogAcciones.CuestionarioInvitadoFin, detalle, ip, ct);

        return new FinalizarCuestionarioResult(c.IdCuestionario, puntaje, nivel, preguntas.Count, porPregunta.Count);
    }

    // Puntaje = promedio de respuestas de escala, redondeado. 1-2 bajo, 3 medio, 4-5 alto.
    // Compartido con PB-08 (cálculo de riesgo). Preguntas no-escala (binaria) no entran.
    public static (int Puntaje, string Nivel) CalcularPuntaje(IEnumerable<int> valoresEscala)
    {
        var vals = valoresEscala.ToList();
        if (vals.Count == 0) return (0, "Bajo");
        var puntaje = (int)Math.Round(vals.Average(), MidpointRounding.AwayFromZero);
        var nivel = puntaje <= 2 ? "Bajo" : puntaje == 3 ? "Medio" : "Alto";
        return (puntaje, nivel);
    }

    // PB-11 HU1-PB11: siguiente cuestionario pendiente para el paciente autenticado.
    // Mismo catálogo que el invitado (Predeterminado + Publicado) menos los que el
    // paciente ya respondió (guardados en su historial, eventos tipo Cuestionario).
    public async Task<CuestionarioPacienteDto> ObtenerPendientePacienteAsync(string? tokenRaw, CancellationToken ct = default)
    {
        var yo = await RequirePacienteAsync(tokenRaw, ct);

        var publicados = await cuestionarios.GetPublicadosInvitadoAsync(ct);
        var respondidos = await historial.GetCuestionariosRespondidosAsync(yo.IdUsuario, ct);
        var pendiente = publicados.FirstOrDefault(c => !respondidos.Contains(c.IdCuestionario));

        if (pendiente is null)
            return new CuestionarioPacienteDto
            {
                Disponible = false,
                Mensaje = "No tienes cuestionarios pendientes por responder."
            };

        var (preguntas, opciones) = await cuestionarios.GetCompletoAsync(pendiente.IdCuestionario, ct);

        return new CuestionarioPacienteDto
        {
            Disponible = true,
            Id = pendiente.IdCuestionario,
            Titulo = pendiente.Titulo,
            Descripcion = pendiente.Descripcion,
            AvisoObligatorio = true,
            Preguntas = preguntas.Select(p => new PreguntaPacienteDto
            {
                Id = p.IdPregunta,
                Texto = p.Texto,
                Orden = p.Orden,
                Opciones = opciones.Where(o => o.IdPregunta == p.IdPregunta)
                    .Select(o => new OpcionPacienteDto { Id = o.IdOpcion, Texto = o.Texto, Valor = o.Valor, Orden = o.Orden })
                    .ToList()
            }).ToList()
        };
    }

    // PB-11 HU1-PB11: registra el inicio del cuestionario del paciente en dbo.auth.
    public async Task RegistrarInicioPacienteAsync(string? tokenRaw, int cuestionarioId, string? ip, CancellationToken ct = default)
    {
        var yo = await RequirePacienteAsync(tokenRaw, ct);
        var c = await cuestionarios.FindPublicadoInvitadoAsync(cuestionarioId, ct);
        var detalle = c is null
            ? $"Paciente:{yo.IdUsuario} inició un cuestionario no disponible (id: {cuestionarioId})"
            : $"Paciente:{yo.IdUsuario} inició el cuestionario \"{c.Titulo}\" (id: {c.IdCuestionario})";
        await authLogs.LogAsync(yo.IdUsuario, yo.IdUsuario, LogAcciones.CuestionarioPacienteInicio, detalle, ip, ct);
    }

    // PB-11 HU1-PB11: valida el 100% (todas obligatorias), calcula puntaje/nivel, persiste
    // las respuestas y el resultado en el historial del paciente (eventos_bienestar +
    // respuestas) y registra la finalización en dbo.auth.
    // Lanza InvalidOperationException con el mensaje exacto para el 400/404.
    public async Task<FinalizarCuestionarioPacienteResult> FinalizarPacienteAsync(string? tokenRaw, FinalizarCuestionarioPacienteVm vm, string? ip, CancellationToken ct = default)
    {
        var yo = await RequirePacienteAsync(tokenRaw, ct);
        var c = await cuestionarios.FindPublicadoInvitadoAsync(vm.CuestionarioId, ct);
        // PB-22: si el paciente tiene asignación pendiente de este cuestionario (sea o no
        // publicado), finalizar la marca Completada.
        var asignacion = await cuestionarios.FindAsignacionPendienteAsync(yo.IdUsuario, vm.CuestionarioId, ct);
        if (c is null && asignacion is null) throw new InvalidOperationException("Cuestionario no disponible.");
        var idCuestionario = c?.IdCuestionario ?? asignacion!.IdCuestionario;
        var titulo = c?.Titulo ?? asignacion!.TituloCuestionario;

        var (preguntas, opciones) = await cuestionarios.GetCompletoAsync(idCuestionario, ct);
        var porPregunta = vm.Respuestas.GroupBy(r => r.PreguntaId).ToDictionary(g => g.Key, g => g.First().OpcionId);

        var valoresEscala = new List<int>();
        var respuestas = new List<RespuestaRow>();
        foreach (var p in preguntas)
        {
            if (!porPregunta.TryGetValue(p.IdPregunta, out var opcionId) || opcionId <= 0)
                throw new InvalidOperationException("Debes seleccionar una respuesta para continuar");
            var op = opciones.FirstOrDefault(o => o.IdOpcion == opcionId && o.IdPregunta == p.IdPregunta);
            if (op is null) throw new InvalidOperationException("La respuesta seleccionada no es válida");
            // Escala: tipo 'escala' u 'opcion_multiple' con valores numéricos (misma regla del invitado).
            if (string.Equals(p.Tipo, "escala", StringComparison.OrdinalIgnoreCase)
                || string.Equals(p.Tipo, "opcion_multiple", StringComparison.OrdinalIgnoreCase))
                valoresEscala.Add(op.Valor);
            respuestas.Add(new RespuestaRow { IdPregunta = p.IdPregunta, Valor = op.Valor, Texto = op.Texto });
        }

        var (puntaje, nivel) = CalcularPuntaje(valoresEscala);

        var idNivel = await historial.GetNivelIdAsync(nivel, ct);
        if (idNivel is null) throw new InvalidOperationException("Nivel de riesgo no configurado.");

        var idEvento = await historial.GuardarEventoCuestionarioAsync(
            yo.IdUsuario, idCuestionario, idNivel.Value, puntaje, respuestas, ct);

        // PB-22: al finalizar, la asignación queda Completada (automático).
        if (asignacion is not null)
            await cuestionarios.MarcarAsignacionCompletadaAsync(asignacion.IdAsignacion, ct);

        var detalle = $"Paciente:{yo.IdUsuario} completó el cuestionario \"{titulo}\" (id: {idCuestionario}) al 100%: puntaje {puntaje}, nivel {nivel}, evento {idEvento}";
        await authLogs.LogAsync(yo.IdUsuario, yo.IdUsuario, LogAcciones.CuestionarioPacienteFin, detalle, ip, ct);

        return new FinalizarCuestionarioPacienteResult(idEvento, idCuestionario, puntaje, nivel, preguntas.Count, porPregunta.Count);
    }

    // PB-22: cuestionarios asignados al paciente (lista para consultar y responder).
    public async Task<List<CuestionarioAsignadoDto>> ObtenerAsignadosPacienteAsync(string? tokenRaw, string? ip, CancellationToken ct = default)
    {
        var yo = await RequirePacienteAsync(tokenRaw, ct);
        var lista = await cuestionarios.GetAsignadosPorPacienteAsync(yo.IdUsuario, ct);

        await authLogs.LogAsync(yo.IdUsuario, yo.IdUsuario, LogAcciones.CuestionarioPacienteInicio,
            $"Paciente:{yo.IdUsuario} consultó sus cuestionarios asignados ({lista.Count})", ip, ct);

        return lista.Select(a => new CuestionarioAsignadoDto
        {
            IdAsignacion = a.IdAsignacion,
            IdCuestionario = a.IdCuestionario,
            Titulo = a.TituloCuestionario,
            Tipo = a.TipoCuestionario,
            FechaAsignacion = a.FechaAsignacion,
            FechaLimite = a.FechaLimite,
            Estado = a.Estado
        }).ToList();
    }

    // PB-22: cuestionario asignado completo (preguntas+opciones) para responderlo.
    public async Task<CuestionarioPacienteDto> ObtenerAsignadoDetalleAsync(string? tokenRaw, int idAsignacion, string? ip, CancellationToken ct = default)
    {
        var yo = await RequirePacienteAsync(tokenRaw, ct);
        var lista = await cuestionarios.GetAsignadosPorPacienteAsync(yo.IdUsuario, ct);
        var asignacion = lista.FirstOrDefault(a => a.IdAsignacion == idAsignacion)
            ?? throw new InvalidOperationException("Asignación no encontrada.");

        var (preguntas, opciones) = await cuestionarios.GetCompletoAsync(asignacion.IdCuestionario, ct);

        await authLogs.LogAsync(yo.IdUsuario, yo.IdUsuario, LogAcciones.CuestionarioPacienteInicio,
            $"Paciente:{yo.IdUsuario} abrió su cuestionario asignado \"{asignacion.TituloCuestionario}\" (asignación {idAsignacion})", ip, ct);

        return new CuestionarioPacienteDto
        {
            Disponible = true,
            Id = asignacion.IdCuestionario,
            Titulo = asignacion.TituloCuestionario,
            Descripcion = null,
            AvisoObligatorio = true,
            Preguntas = preguntas.Select(p => new PreguntaPacienteDto
            {
                Id = p.IdPregunta,
                Texto = p.Texto,
                Orden = p.Orden,
                Opciones = opciones.Where(o => o.IdPregunta == p.IdPregunta)
                    .Select(o => new OpcionPacienteDto { Id = o.IdOpcion, Texto = o.Texto, Valor = o.Valor, Orden = o.Orden })
                    .ToList()
            }).ToList()
        };
    }

    // Requiere sesión de paciente válida (mismo patrón que HistorialService).
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

    private async Task<int?> ResolveActorIdAsync(string? tokenRaw, CancellationToken ct)
    {
        if (string.IsNullOrWhiteSpace(tokenRaw)) return null;
        try
        {
            var hash = TokenHelper.Hash(tokenRaw.Trim());
            var sesion = await sesiones.FindByTokenHashAsync(hash, ct);
            return sesion?.IdUsuario;
        }
        catch
        {
            return null;
        }
    }

    private string RolOrInvitado(int? actorId) => actorId is null ? "Invitado" : $"Usuario:{actorId}";
}
