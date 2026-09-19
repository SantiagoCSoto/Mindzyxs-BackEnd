using Microsoft.AspNetCore.Mvc;
using Mindzyxs_API.Dtos;
using Mindzyxs_API.Services;

namespace Mindzyxs_API.Controllers;

/// <summary>
/// Modo invitado (PB-06 HU1-PB6): acceso sin cuenta desde la Landing Page, configuración
/// del Panel de invitado (opciones disponibles vs exclusivas), y registro en `dbo.auth` de
/// accesos, salidas e intentos bloqueados sobre funciones de usuarios registrados.
/// Endpoints públicos (no requieren autenticación): el invitado no tiene sesión.
/// </summary>
[ApiController]
[Route("api/[controller]")]
public class InvitadoController(InvitadoService invitado) : ControllerBase
{
    /// <summary>Devuelve la configuración del Panel de invitado (opciones básicas y funciones exclusivas).</summary>
    /// <remarks>
    /// PB-06 HU1-PB6. Público. Usado por el front para renderizar "Inicio (Invitado)":
    /// las 3 opciones disponibles (Responder cuestionario, Ver recomendaciones básicas,
    /// Líneas de ayuda) y las 4 funciones exclusivas de usuarios registrados (Historial
    /// emocional, Gráficos de progreso, Recordatorios, Autorización de psicólogo), que deben
    /// mostrarse bloqueadas. `avisoObligatorio` indica si el mensaje aclaratorio de no
    /// diagnóstico se exige antes de las funciones sensibles (constante del sistema).
    ///
    /// Respuestas:
    /// - `200` — `{ "avisoObligatorio": true, "disponibles": [...], "exclusivas": [...] }`
    /// </remarks>
    /// <response code="200">Configuración del panel de invitado.</response>
    [HttpGet("panel")]
    [ProducesResponseType(typeof(InvitadoPanelDto), StatusCodes.Status200OK)]
    public async Task<ActionResult<InvitadoPanelDto>> Panel(CancellationToken ct)
        => Ok(await invitado.ObtenerPanelAsync(ct));

    /// <summary>Registra el acceso del usuario al modo invitado (o su error de carga) en `dbo.auth`.</summary>
    /// <remarks>
    /// PB-06 HU1-PB6. Público. El front lo invoca al presionar "Modo invitado" en la Landing Page.
    /// Guarda el acceso como `INVITADO_ACCESO` (o `INVITADO_ACCESO_ERROR` con `resultado: "error"`)
    /// en `dbo.auth`. Sin bloqueo de UI: si el endpoint falla (red/back caído), el front debe
    /// mostrar "No fue posible acceder en modo invitado. Intente nuevamente".
    ///
    /// Respuestas:
    /// - `200` — `{ "mensaje": "Acceso registrado" }`
    /// - `400` — `{ "error": "Este campo es obligatorio" }` (body inválido/vacío)
    /// </remarks>
    /// <response code="200">Acceso en modo invitado registrado.</response>
    /// <response code="400">Body inválido.</response>
    [HttpPost("acceso")]
    [ProducesResponseType(typeof(MessageResult), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ErrorResult), StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> Acceso([FromBody] AccesoInvitadoVm vm, CancellationToken ct)
    {
        if (!ModelState.IsValid) return BadRequest(new { error = "Este campo es obligatorio" });

        var ip = HttpContext.Connection.RemoteIpAddress?.ToString();
        var token = ReadOptionalBearerToken();

        await invitado.RegistrarAccesoAsync(token, vm.Origen, vm.Resultado, ip, ct);
        return Ok(new { mensaje = "Acceso registrado" });
    }

    /// <summary>Registra en `dbo.auth` el intento bloqueado de usar una función exclusiva de usuarios registrados.</summary>
    /// <remarks>
    /// PB-06 HU1-PB6. Público. El front lo invoca cuando el invitado intenta usar una función
    /// exclusiva (historial emocional, gráficos de progreso, recordatorios, autorización de psicólogo)
    /// y el sistema la bloquea mostrando el aviso "requiere crear una cuenta o iniciar sesión".
    /// Guarda `INVITADO_BLOQUEO` en `dbo.auth` con el nombre de la función.
    ///
    /// Respuestas:
    /// - `200` — `{ "mensaje": "Intento registrado" }`
    /// - `400` — `{ "error": "Este campo es obligatorio" }` (body inválido/vacío)
    /// </remarks>
    /// <response code="200">Intento bloqueado registrado.</response>
    /// <response code="400">Body inválido.</response>
    [HttpPost("bloqueo")]
    [ProducesResponseType(typeof(MessageResult), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ErrorResult), StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> Bloqueo([FromBody] BloqueoInvitadoVm vm, CancellationToken ct)
    {
        if (!ModelState.IsValid) return BadRequest(new { error = "Este campo es obligatorio" });

        var ip = HttpContext.Connection.RemoteIpAddress?.ToString();
        var token = ReadOptionalBearerToken();

        await invitado.RegistrarBloqueoAsync(token, vm.Funcion, ip, ct);
        return Ok(new { mensaje = "Intento registrado" });
    }

    /// <summary>Registra en `dbo.auth` la salida del modo invitado (vuelta a la Landing Page).</summary>
    /// <remarks>
    /// PB-06 HU1-PB6. Público. El front lo invoca cuando el invitado presiona "← Volver a la Landing"
    /// en el panel. Guarda `INVITADO_SALIDA` en `dbo.auth`.
    ///
    /// Respuestas:
    /// - `200` — `{ "mensaje": "Salida registrada" }`
    /// - `400` — `{ "error": "Este campo es obligatorio" }` (body inválido/vacío)
    /// </remarks>
    /// <response code="200">Salida del modo invitado registrada.</response>
    /// <response code="400">Body inválido.</response>
    [HttpPost("salida")]
    [ProducesResponseType(typeof(MessageResult), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ErrorResult), StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> Salida([FromBody] SalidaInvitadoVm vm, CancellationToken ct)
    {
        if (!ModelState.IsValid) return BadRequest(new { error = "Este campo es obligatorio" });

        var ip = HttpContext.Connection.RemoteIpAddress?.ToString();
        var token = ReadOptionalBearerToken();

        await invitado.RegistrarSalidaAsync(token, vm.Origen, ip, ct);
        return Ok(new { mensaje = "Salida registrada" });
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