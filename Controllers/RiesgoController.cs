using Microsoft.AspNetCore.Mvc;
using Mindzyxs_API.Dtos;
using Mindzyxs_API.Services;

namespace Mindzyxs_API.Controllers;

/// <summary>
/// Riesgo emocional del invitado (PB-08 HU1-PB8): procesa las respuestas completas
/// del cuestionario finalizado, calcula el nivel (Bajo/Medio/Alto) y arma la pantalla
/// Resultado del riesgo con explicación, alerta de urgencia si es Alto y accesos a
/// recomendaciones y líneas de ayuda. Endpoints públicos (el invitado no tiene sesión).
/// Riesgo emocional del paciente (PB-16 HU1-PB16): registra en logs la alerta de
/// riesgo alto (generación/visualización y la acción elegida). El cálculo del nivel
/// ya lo entregan finalizar (PB-11) y registrar estado (PB-12).
/// </summary>
[ApiController]
[Route("api/[controller]")]
public class RiesgoController(RiesgoService riesgo) : ControllerBase
{
    /// <summary>Calcula el nivel de riesgo emocional desde las respuestas de la sesión.</summary>
    /// <remarks>
    /// PB-08 HU1-PB8. Público. El front lo invoca con el botón "Ver resultado" (pantalla
    /// Cuestionario completado), reenviando las respuestas guardadas en su sesión temporal.
    /// Puntaje = promedio de escala redondeado (1-2 Bajo, 3 Medio, 4-5 Alto).
    /// Si es Alto, `alerta.mostrar=true` y el front resalta "Líneas de ayuda".
    /// Guarda `RIESGO_INVITADO_CALCULO` en `dbo.auth`.
    ///
    /// Respuestas:
    /// - `200` — `{ "nivel": "Medio", "puntaje": 3, "explicacion": "...", "alerta": {...}, "mostrarLineasAyuda": true, "mostrarRecomendaciones": true, "avisoObligatorio": true }`
    /// - `400` — `{ "error": "No se encontraron respuestas para calcular el riesgo. Por favor responde el cuestionario nuevamente" }` (sesión expirada o no finalizado; el front redirige al panel/cuestionario)
    /// - `400` — `{ "error": "Este campo es obligatorio" }` (body inválido/vacío)
    /// - `404` — `{ "error": "Cuestionario no disponible." }`
    /// </remarks>
    /// <response code="200">Nivel de riesgo calculado.</response>
    /// <response code="400">Sin respuestas completas o body inválido.</response>
    /// <response code="404">Cuestionario no publicado.</response>
    [HttpPost("invitado/calcular")]
    [ProducesResponseType(typeof(RiesgoInvitadoDto), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ErrorResult), StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> Calcular([FromBody] CalcularRiesgoVm vm, CancellationToken ct)
    {
        if (!ModelState.IsValid || vm.CuestionarioId <= 0 || vm.Respuestas is null)
            return BadRequest(new { error = "Este campo es obligatorio" });

        var ip = HttpContext.Connection.RemoteIpAddress?.ToString();
        try
        {
            var dto = await riesgo.CalcularAsync(ReadOptionalBearerToken(), vm, ip, ct);
            return Ok(dto);
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

    /// <summary>Registra en logs la alerta de riesgo alto del paciente.</summary>
    /// <remarks>
    /// PB-16 HU1-PB16. Solo paciente autenticado (Bearer). El nivel ya viene de
    /// finalizar (PB-11) o registrar estado (PB-12): si `nivel == "Alto"` el front
    /// muestra el modal (mensaje fijo del HU + botones Líneas de ayuda, Ver
    /// recomendaciones y Entendido) y registra aquí la generación/visualización
    /// ("vista") y la acción elegida (lineas/recomendaciones/entendido). Con reset
    /// Bajo/Medio no se invoca (el HU no muestra la alerta). Guarda
    /// `ALERTA_RIESGO_ALTO_VISTA` / `ALERTA_RIESGO_ALTO_ACCION` en `dbo.auth`.
    ///
    /// Respuestas:
    /// - `200` — `{ "mensaje": "Alerta registrada" }`
    /// - `400` — `{ "error": "Debes seleccionar una opción de la alerta de riesgo" }`
    /// - `400` — `{ "error": "Acción de la alerta no válida" }`
    /// - `401` — `{ "error": "Sesión requerida" }`
    /// </remarks>
    /// <response code="200">Alerta registrada.</response>
    /// <response code="400">Acción faltante o no válida.</response>
    /// <response code="401">Sesión de paciente requerida.</response>
    [HttpPost("paciente/alerta")]
    [ProducesResponseType(typeof(ErrorResult), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ErrorResult), StatusCodes.Status401Unauthorized)]
    public async Task<IActionResult> RegistrarAlerta([FromBody] RegistrarAlertaRiesgoVm vm, CancellationToken ct)
    {
        var ip = HttpContext.Connection.RemoteIpAddress?.ToString();
        try
        {
            var mensaje = await riesgo.RegistrarAlertaAsync(ReadOptionalBearerToken(), vm, ip, ct);
            return Ok(new { mensaje });
        }
        catch (UnauthorizedAccessException ex)
        {
            return Unauthorized(new { error = ex.Message });
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(new { error = ex.Message });
        }
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
