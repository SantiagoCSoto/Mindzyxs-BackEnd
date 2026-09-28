using Mindzyxs_API.Dtos;
using Mindzyxs_API.Repositories;
using Mindzyxs_API.Utilities;

namespace Mindzyxs_API.Services;

public class CuestionarioService(
    ICuestionarioRepository cuestionarios,
    ISesionRepository sesiones,
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
