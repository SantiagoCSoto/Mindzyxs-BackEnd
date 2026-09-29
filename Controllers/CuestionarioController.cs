using Microsoft.AspNetCore.Mvc;
using Mindzyxs_API.Dtos;
using Mindzyxs_API.Services;

namespace Mindzyxs_API.Controllers;

/// <summary>
/// Cuestionario emocional del invitado (PB-07 HU1-PB7): catálogo dinámico desde BD
/// (Predeterminado + Publicado, rotación por excluidos), registro de inicio y
/// finalización con validación del 100% + puntaje/nivel en `dbo.auth`.
/// Endpoints públicos (no requieren autenticación): el invitado no tiene sesión.
/// Las respuestas NO se persisten (respuestas→evento→paciente NOT NULL): el front
/// las guarda en su sesión (cookies) y las reusa en PB-08.
///
/// Cuestionario emocional del paciente (PB-11 HU1-PB11): mismos catálogo y validaciones,
/// pero requiere sesión de paciente y al finalizar PERSISTE las respuestas y el resultado
/// en el historial del paciente (`eventos_bienestar` + `respuestas`, reutilizados por PB-13).
///
/// Asignados del paciente (PB-22): el psicólogo consulta `dbo.asignaciones_cuestionario`
/// (`GET psicologo/paciente/{id}/asignados`); el crear-asignación llega en sprint 4.
/// </summary>
[ApiController]
[Route("api/[controller]")]
public class CuestionarioController(CuestionarioService cuestionario) : ControllerBase
{
    /// <summary>Devuelve el siguiente cuestionario pendiente (rotación por excluidos).</summary>
    /// <remarks>
    /// PB-07 HU1-PB7. Público. `excluidos` = ids ya hechos en la sesión (cookies `cuest_*`),
    /// separados por coma (ej. `?excluidos=1,2`). Responde el primer publicado no excluido
    /// con sus N preguntas y M opciones ordenadas. Todas las preguntas son obligatorias.
    ///
    /// Respuestas:
    /// - `200` — `{ "disponible": true, "id": ..., "titulo": ..., "avisoObligatorio": true, "preguntas": [...] }`
    /// - `200` — `{ "disponible": false, "mensaje": "No tienes cuestionarios pendientes por responder." }`
    /// </remarks>
    /// <response code="200">Cuestionario pendiente o estado sin pendientes.</response>
    [HttpGet("invitado")]
    [ProducesResponseType(typeof(CuestionarioInvitadoDto), StatusCodes.Status200OK)]
    public async Task<ActionResult<CuestionarioInvitadoDto>> Pendiente([FromQuery] string? excluidos, CancellationToken ct)
        => Ok(await cuestionario.ObtenerPendienteAsync(ParseExcluidos(excluidos), ct));

    /// <summary>Registra el inicio del cuestionario en `dbo.auth`.</summary>
    /// <remarks>
    /// PB-07 HU1-PB7. Público. El front lo invoca al iniciar (tras el aviso con Entendido).
    /// Guarda `CUESTIONARIO_INVITADO_INICIO` en `dbo.auth`. No bloquea la UI.
    ///
    /// Respuestas:
    /// - `200` — `{ "mensaje": "Inicio registrado" }`
    /// - `400` — `{ "error": "Este campo es obligatorio" }` (body inválido/vacío)
    /// </remarks>
    /// <response code="200">Inicio del cuestionario registrado.</response>
    /// <response code="400">Body inválido.</response>
    [HttpPost("invitado/inicio")]
    [ProducesResponseType(typeof(MessageResult), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ErrorResult), StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> Inicio([FromBody] InicioCuestionarioVm vm, CancellationToken ct)
    {
        if (!ModelState.IsValid || vm.CuestionarioId <= 0)
            return BadRequest(new { error = "Este campo es obligatorio" });

        var ip = HttpContext.Connection.RemoteIpAddress?.ToString();
        await cuestionario.RegistrarInicioAsync(ReadOptionalBearerToken(), vm.CuestionarioId, ip, ct);
        return Ok(new { mensaje = "Inicio registrado" });
    }

    /// <summary>Valida el 100%, calcula puntaje/nivel y registra la finalización en `dbo.auth`.</summary>
    /// <remarks>
    /// PB-07 HU1-PB7. Público. El front envía TODAS las respuestas (sesión temporal).
    /// Valida que cada pregunta tenga respuesta ("Debes seleccionar una respuesta para continuar")
    /// y que cada opción pertenezca a su pregunta. Puntaje = promedio de escala redondeado
    /// (1-2 bajo, 3 medio, 4-5 alto). El front marca `cuest_{id}` como hecho en cookies.
    ///
    /// Respuestas:
    /// - `200` — `{ "mensaje": "Cuestionario completado", "cuestionarioId": ..., "puntaje": ..., "nivel": ..., "totalPreguntas": ..., "respondidas": ... }`
    /// - `400` — `{ "error": "Debes seleccionar una respuesta para continuar" }` (falta alguna)
    /// - `400` — `{ "error": "Este campo es obligatorio" }` (body inválido/vacío)
    /// - `404` — `{ "error": "Cuestionario no disponible." }`
    /// </remarks>
    /// <response code="200">Cuestionario completado al 100%.</response>
    /// <response code="400">Faltan respuestas o body inválido.</response>
    /// <response code="404">Cuestionario no publicado.</response>
    [HttpPost("invitado/finalizar")]
    [ProducesResponseType(typeof(FinalizarCuestionarioResult), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ErrorResult), StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> Finalizar([FromBody] FinalizarCuestionarioVm vm, CancellationToken ct)
    {
        if (!ModelState.IsValid || vm.CuestionarioId <= 0 || vm.Respuestas is null)
            return BadRequest(new { error = "Este campo es obligatorio" });

        var ip = HttpContext.Connection.RemoteIpAddress?.ToString();
        try
        {
            var r = await cuestionario.FinalizarAsync(ReadOptionalBearerToken(), vm, ip, ct);
            return Ok(new
            {
                mensaje = "Cuestionario completado",
                cuestionarioId = r.CuestionarioId,
                puntaje = r.Puntaje,
                nivel = r.Nivel,
                totalPreguntas = r.TotalPreguntas,
                respondidas = r.Respondidas
            });
        }
        catch (InvalidOperationException ex) when (ex.Message == "Cuestionario no disponible.")
        {
            return NotFound(new { error = ex.Message });
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(new { error = ex.Message });
        }
    }

    /// <summary>Devuelve el siguiente cuestionario pendiente del paciente (sin repetir los ya respondidos).</summary>
    /// <remarks>
    /// PB-11 HU1-PB11. Requiere sesión de paciente. Mismo catálogo que el invitado
    /// (Predeterminado + Publicado) menos los que el paciente ya respondió (guardados
    /// en su historial). El front muestra la pantalla Cuestionario emocional con las
    /// preguntas una por una (Siguiente / Cancelar-Volver / Finalizar).
    ///
    /// Respuestas:
    /// - `200` — `{ "disponible": true, "id": ..., "titulo": ..., "avisoObligatorio": true, "preguntas": [...] }`
    /// - `200` — `{ "disponible": false, "mensaje": "No tienes cuestionarios pendientes por responder." }`
    /// - `401` — sin sesión.
    /// </remarks>
    /// <response code="200">Cuestionario pendiente o estado sin pendientes.</response>
    /// <response code="401">Sesión requerida.</response>
    [HttpGet("paciente/pendiente")]
    [ProducesResponseType(typeof(CuestionarioPacienteDto), StatusCodes.Status200OK)]
    public async Task<IActionResult> PendientePaciente(CancellationToken ct)
    {
        try
        {
            return Ok(await cuestionario.ObtenerPendientePacienteAsync(ReadOptionalBearerToken(), ct));
        }
        catch (UnauthorizedAccessException ex)
        {
            return Unauthorized(new { error = ex.Message });
        }
    }

    /// <summary>Registra el inicio del cuestionario del paciente en `dbo.auth`.</summary>
    /// <remarks>
    /// PB-11 HU1-PB11. Requiere sesión de paciente. Se invoca al iniciar (tras el aviso
    /// con Entendido). Guarda `CUESTIONARIO_PACIENTE_INICIO` en `dbo.auth`. No bloquea la UI.
    ///
    /// Respuestas:
    /// - `200` — `{ "mensaje": "Inicio registrado" }`
    /// - `400` — `{ "error": "Este campo es obligatorio" }` (body inválido/vacío)
    /// - `401` — sin sesión.
    /// </remarks>
    /// <response code="200">Inicio del cuestionario registrado.</response>
    /// <response code="400">Body inválido.</response>
    /// <response code="401">Sesión requerida.</response>
    [HttpPost("paciente/inicio")]
    [ProducesResponseType(typeof(MessageResult), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ErrorResult), StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> InicioPaciente([FromBody] InicioCuestionarioVm vm, CancellationToken ct)
    {
        if (!ModelState.IsValid || vm.CuestionarioId <= 0)
            return BadRequest(new { error = "Este campo es obligatorio" });

        var ip = HttpContext.Connection.RemoteIpAddress?.ToString();
        try
        {
            await cuestionario.RegistrarInicioPacienteAsync(ReadOptionalBearerToken(), vm.CuestionarioId, ip, ct);
            return Ok(new { mensaje = "Inicio registrado" });
        }
        catch (UnauthorizedAccessException ex)
        {
            return Unauthorized(new { error = ex.Message });
        }
    }

    /// <summary>Valida el 100%, calcula puntaje/nivel y guarda respuestas + resultado en el historial del paciente.</summary>
    /// <remarks>
    /// PB-11 HU1-PB11. Requiere sesión de paciente. El front envía TODAS las respuestas
    /// (avance secuencial con Siguiente). Valida que cada pregunta tenga respuesta
    /// ("Debes seleccionar una respuesta para continuar") y que cada opción pertenezca
    /// a su pregunta. Puntaje = promedio de escala redondeado (1-2 bajo, 3 medio, 4-5 alto).
    /// Persiste el evento en `eventos_bienestar` y cada respuesta en `respuestas` (historial,
    /// reutilizado por PB-13) y guarda `CUESTIONARIO_PACIENTE_FIN` en `dbo.auth`.
    ///
    /// Respuestas:
    /// - `200` — `{ "mensaje": "Cuestionario completado", "idEvento": ..., "cuestionarioId": ..., "puntaje": ..., "nivel": ..., "totalPreguntas": ..., "respondidas": ... }`
    /// - `400` — `{ "error": "Debes seleccionar una respuesta para continuar" }` (falta alguna)
    /// - `400` — `{ "error": "Este campo es obligatorio" }` (body inválido/vacío)
    /// - `401` — sin sesión.
    /// - `404` — `{ "error": "Cuestionario no disponible." }`
    /// </remarks>
    /// <response code="200">Cuestionario completado y guardado en historial.</response>
    /// <response code="400">Faltan respuestas o body inválido.</response>
    /// <response code="401">Sesión requerida.</response>
    /// <response code="404">Cuestionario no publicado.</response>
    [HttpPost("paciente/finalizar")]
    [ProducesResponseType(typeof(FinalizarCuestionarioPacienteResult), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ErrorResult), StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> FinalizarPaciente([FromBody] FinalizarCuestionarioPacienteVm vm, CancellationToken ct)
    {
        if (!ModelState.IsValid || vm.CuestionarioId <= 0 || vm.Respuestas is null)
            return BadRequest(new { error = "Este campo es obligatorio" });

        var ip = HttpContext.Connection.RemoteIpAddress?.ToString();
        try
        {
            var r = await cuestionario.FinalizarPacienteAsync(ReadOptionalBearerToken(), vm, ip, ct);
            return Ok(new
            {
                mensaje = "Cuestionario completado",
                idEvento = r.IdEvento,
                cuestionarioId = r.CuestionarioId,
                puntaje = r.Puntaje,
                nivel = r.Nivel,
                totalPreguntas = r.TotalPreguntas,
                respondidas = r.Respondidas
            });
        }
        catch (UnauthorizedAccessException ex)
        {
            return Unauthorized(new { error = ex.Message });
        }
        catch (InvalidOperationException ex) when (ex.Message == "Cuestionario no disponible.")
        {
            return NotFound(new { error = ex.Message });
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(new { error = ex.Message });
        }
    }

    /// <summary>Devuelve los cuestionarios asignados a un paciente específico.</summary>
    /// <remarks>
    /// PB-22 (consulta; el crear-asignación llega en sprint 4). Requiere sesión de
    /// psicólogo (o admin) y paciente autorizado. Lee `dbo.asignaciones_cuestionario`.
    ///
    /// Respuestas:
    /// - `200` — `[ { "idAsignacion", "idCuestionario", "titulo", "tipo", "fechaAsignacion", "fechaLimite", "estado" } ]` (vacía si no hay)
    /// - `401` — sin sesión.
    /// - `403` — sin rol o sin autorización sobre el paciente.
    /// - `404` — `{ "error": "Paciente no encontrado." }`
    /// </remarks>
    /// <response code="200">Asignaciones del paciente.</response>
    /// <response code="401">Sesión requerida.</response>
    /// <response code="403">Sin permisos o sin autorización.</response>
    /// <response code="404">Paciente no encontrado.</response>
    [HttpGet("psicologo/paciente/{idPaciente:int}/asignados")]
    [ProducesResponseType(typeof(List<CuestionarioAsignadoDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ErrorResult), StatusCodes.Status403Forbidden)]
    [ProducesResponseType(typeof(ErrorResult), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> AsignadosPaciente(int idPaciente, CancellationToken ct)
    {
        var ip = HttpContext.Connection.RemoteIpAddress?.ToString();
        try
        {
            return Ok(await cuestionario.ObtenerAsignadosAsync(ReadOptionalBearerToken(), idPaciente, ip, ct));
        }
        catch (UnauthorizedAccessException ex)
        {
            return Unauthorized(new { error = ex.Message });
        }
        catch (InvalidOperationException ex) when (ex.Message == "Paciente no encontrado.")
        {
            return NotFound(new { error = ex.Message });
        }
        catch (InvalidOperationException ex)
        {
            return StatusCode(StatusCodes.Status403Forbidden, new { error = ex.Message });
        }
    }

    /// <summary>Parsea `?excluidos=1,2` a conjunto de ids (ignora no numéricos).</summary>
    private static HashSet<int> ParseExcluidos(string? excluidos)
    {
        var set = new HashSet<int>();
        foreach (var part in (excluidos ?? "").Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries))
            if (int.TryParse(part, out var id) && id > 0) set.Add(id);
        return set;
    }

    /// <summary>Lee opcionalmente el token de sesión (no requiere ser válido para el registro).</summary>
    private string? ReadOptionalBearerToken()
    {
        if (!Request.Headers.TryGetValue("Authorization", out var v)) return null;
        var parts = v.ToString().Trim().Split(' ', StringSplitOptions.RemoveEmptyEntries);
        var token = parts.Length == 1 ? parts[0] : parts[^1];
        return token.Trim().Trim('"').Trim('\'');
    }
}
