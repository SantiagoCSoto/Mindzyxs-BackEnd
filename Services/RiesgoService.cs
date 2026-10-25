using Mindzyxs_API.Dtos;
using Mindzyxs_API.Repositories;
using Mindzyxs_API.Utilities;

namespace Mindzyxs_API.Services;

public class RiesgoService(
    ICuestionarioRepository cuestionarios,
    ILineaAyudaRepository lineas,
    ISesionRepository sesiones,
    IAuthLogRepository authLogs,
    IUserRepository usuarios)
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

        // Corrección BD: preguntas+opciones en 1 round-trip (sp_cuestionario_completo).
        var (preguntas, opciones) = await cuestionarios.GetCompletoAsync(c.IdCuestionario, ct);
        var porPregunta = vm.Respuestas.GroupBy(r => r.PreguntaId).ToDictionary(g => g.Key, g => g.First().OpcionId);

        // Sin sesión expirada / cuestionario no finalizado -> mensaje HU + redirect del front.
        if (preguntas.Any(p => !porPregunta.TryGetValue(p.IdPregunta, out var op) || op <= 0))
            throw new InvalidOperationException("No se encontraron respuestas para calcular el riesgo. Por favor responde el cuestionario nuevamente");

        var valoresEscala = new List<int>();
        foreach (var p in preguntas)
        {
            var op = opciones.FirstOrDefault(o => o.IdOpcion == porPregunta[p.IdPregunta] && o.IdPregunta == p.IdPregunta);
            if (op is null) throw new InvalidOperationException("La respuesta seleccionada no es válida");
            // Escala: tipo 'escala' u 'opcion_multiple' con valores numéricos
            // (el cuestionario 1 usa opcion_multiple). 'binaria' no entra.
            if (string.Equals(p.Tipo, "escala", StringComparison.OrdinalIgnoreCase)
                || string.Equals(p.Tipo, "opcion_multiple", StringComparison.OrdinalIgnoreCase))
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
                Mensaje = esAlto ? "Por lo que nos has contado, sería importante buscar apoyo profesional pronto. Aquí tienes opciones de ayuda inmediata." : ""
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
            Descripcion = l.Descripcion,
            Whatsapp = l.Whatsapp,
            Email = l.Email,
            ChatUrl = l.ChatUrl,
            Videollamada = l.Videollamada,
            TipoAtencion = l.TipoAtencion,
            Departamento = l.Departamento,
            Municipio = l.Municipio,
            Fuente = l.Fuente
        }).ToList();
    }

    // Breve explicación general por nivel (no diagnóstico clínico).
    private static string ExplicacionPorNivel(string nivel) => nivel switch
    {
        "Medio" => "Se observan algunos síntomas emocionales que conviene vigilar. Te recomendamos mantener un monitoreo periódico y revisar las recomendaciones básicas para tu bienestar. Si lo necesitas, también puedes buscar apoyo profesional.",
        "Alto" => "Se observan síntomas emocionales relevantes en tus respuestas. Es muy importante que busques ayuda profesional o te comuniques con una línea de ayuda de tu país. Esto no es un diagnóstico clínico.",
        _ => "Tus respuestas no muestran señales de alto riesgo en este momento. Te invitamos a continuar con tu monitoreo habitual y registrar cómo te sientes periódicamente. Si en algún momento te sientes mal, busca apoyo profesional."
    };

    // PB-16 HU1-PB16: registra en dbo.auth la alerta de riesgo alto del paciente.
    // "vista" cubre la generación y la visualización del modal; lineas/recomendaciones/
    // entendido registran la acción elegida. Exige sesión de paciente (401 si no).
    // Lanza InvalidOperationException con el mensaje exacto para el 400.
    public async Task<string> RegistrarAlertaAsync(string? tokenRaw, RegistrarAlertaRiesgoVm vm, string? ip, CancellationToken ct = default)
    {
        var yo = await RequirePacienteAsync(tokenRaw, ct);

        var accion = vm.Accion?.Trim() ?? "";
        if (string.IsNullOrEmpty(accion))
            throw new InvalidOperationException("Debes seleccionar una opción de la alerta de riesgo");
        if (!EsAccionValida(accion))
            throw new InvalidOperationException("Acción de la alerta no válida");

        var origen = string.IsNullOrWhiteSpace(vm.Origen) ? "" : vm.Origen.Trim();
        var detalleOrigen = string.IsNullOrEmpty(origen) ? "" : $" (origen: {origen})";

        if (string.Equals(accion, "vista", StringComparison.OrdinalIgnoreCase))
            await authLogs.LogAsync(yo.IdUsuario, yo.IdUsuario, LogAcciones.AlertaRiesgoVista,
                $"Se generó y visualizó la alerta de riesgo alto del paciente {yo.IdUsuario}{detalleOrigen}", ip, ct);
        else
            await authLogs.LogAsync(yo.IdUsuario, yo.IdUsuario, LogAcciones.AlertaRiesgoAccion,
                $"Paciente {yo.IdUsuario} eligió \"{EtiquetaAccion(accion)}\" en la alerta de riesgo alto{detalleOrigen}", ip, ct);

        return "Alerta registrada";
    }

    // Acciones válidas de la alerta (HU1-PB16: Líneas de ayuda, Ver recomendaciones, Entendido + vista).
    private static bool EsAccionValida(string accion) => accion switch
    {
        "vista" or "lineas" or "recomendaciones" or "entendido" => true,
        _ => false
    };

    // Etiqueta legible de cada acción para el detalle del log.
    private static string EtiquetaAccion(string accion) => accion switch
    {
        "lineas" => "Líneas de ayuda",
        "recomendaciones" => "Ver recomendaciones",
        "entendido" => "Entendido",
        _ => accion
    };

    // PB-16 HU1-PB16: exige sesión activa de paciente (mismo criterio que HistorialService PB-12/13).
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
