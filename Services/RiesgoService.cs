using Mindzyxs_API.Dtos;
using Mindzyxs_API.Repositories;
using Mindzyxs_API.Utilities;

namespace Mindzyxs_API.Services;

public class RiesgoService(
    ICuestionarioRepository cuestionarios,
    ILineaAyudaRepository lineas,
    ISesionRepository sesiones,
    IAuthLogRepository authLogs)
{
    // PB-08 HU1-PB8: procesa las respuestas completas y calcula el nivel de riesgo.
    // Puntaje = promedio de escala (reusa CuestionarioService.CalcularPuntaje).
    // Sin respuestas completas -> mensaje exacto de la HU. Registra cálculo en dbo.auth.
    // Lanza InvalidOperationException con el mensaje exacto para el 400.
    public async Task<RiesgoInvitadoDto> CalcularAsync(string? tokenRaw, CalcularRiesgoVm vm, string? ip, CancellationToken ct = default)
    {
        var actorId = await ResolveActorIdAsync(tokenRaw, ct);
        var c = await cuestionarios.FindPublicadoInvitadoAsync(vm.CuestionarioId, ct);
        if (c is null) throw new InvalidOperationException("Cuestionario no disponible.");

        var preguntas = await cuestionarios.GetPreguntasAsync(c.IdCuestionario, ct);
        var opciones = await cuestionarios.GetOpcionesAsync(c.IdCuestionario, ct);
        var porPregunta = vm.Respuestas.GroupBy(r => r.PreguntaId).ToDictionary(g => g.Key, g => g.First().OpcionId);

        // Sin sesión expirada / cuestionario no finalizado -> mensaje HU + redirect del front.
        if (preguntas.Any(p => !porPregunta.TryGetValue(p.IdPregunta, out var op) || op <= 0))
            throw new InvalidOperationException("No se encontraron respuestas para calcular el riesgo. Por favor responde el cuestionario nuevamente");

        var valoresEscala = new List<int>();
        foreach (var p in preguntas)
        {
            var op = opciones.FirstOrDefault(o => o.IdOpcion == porPregunta[p.IdPregunta] && o.IdPregunta == p.IdPregunta);
            if (op is null) throw new InvalidOperationException("La respuesta seleccionada no es válida");
            if (string.Equals(p.Tipo, "escala", StringComparison.OrdinalIgnoreCase))
                valoresEscala.Add(op.Valor);
        }

        var (puntaje, nivel) = CuestionarioService.CalcularPuntaje(valoresEscala);
        var esAlto = nivel == "Alto";
        var dto = new RiesgoInvitadoDto
        {
            Nivel = nivel,
            Puntaje = puntaje,
            Explicacion = ExplicacionPorNivel(nivel),
            Alerta = new AlertaRiesgoDto
            {
                Mostrar = esAlto,
                Mensaje = esAlto ? "Riesgo alto detectado. Busca ayuda profesional o comunícate con una línea de ayuda" : ""
            },
            MostrarLineasAyuda = true,
            MostrarRecomendaciones = true,
            AvisoObligatorio = true
        };

        var detalle = $"{RolOrInvitado(actorId)} calculó su riesgo (cuestionario \"{c.Titulo}\", id: {c.IdCuestionario}): puntaje {puntaje}, nivel {nivel}"
            + (esAlto ? " — ALERTA de urgencia mostrada" : "");
        await authLogs.LogAsync(actorId, actorId, LogAcciones.RiesgoInvitadoCalculo, detalle, ip, ct);

        return dto;
    }

    // PB-08 HU1-PB8: líneas de ayuda activas (botón + modal, resaltadas si riesgo alto).
    public async Task<List<LineaAyudaDto>> ObtenerLineasAsync(CancellationToken ct = default)
    {
        var rows = await lineas.GetActivasAsync(ct);
        return rows.Select(l => new LineaAyudaDto
        {
            Id = l.IdLinea,
            Nombre = l.Nombre,
            Telefono = l.Telefono,
            Url = l.Url,
            Horario = l.Horario,
            Cobertura = l.Cobertura,
            Descripcion = l.Descripcion
        }).ToList();
    }

    // Breve explicación general por nivel (no diagnóstico clínico).
    private static string ExplicacionPorNivel(string nivel) => nivel switch
    {
        "Medio" => "Se observan algunos síntomas emocionales que conviene vigilar. Te recomendamos mantener un monitoreo periódico y revisar las recomendaciones básicas para tu bienestar.",
        "Alto" => "Se observan síntomas emocionales relevantes en tus respuestas. Es muy importante que busques ayuda profesional o te comuniques con una línea de ayuda de tu país.",
        _ => "No se observan síntomas emocionales significativos en tus respuestas. Te invitamos a continuar con tu monitoreo habitual y registrar cómo te sientes periódicamente."
    };

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
