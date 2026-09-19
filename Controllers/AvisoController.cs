using Microsoft.AspNetCore.Mvc;
using Mindzyxs_API.Dtos;
using Mindzyxs_API.Services;

namespace Mindzyxs_API.Controllers;

/// <summary>
/// Mensaje aclaratorio (PB-04 HU1-PB4): contenido del aviso de que Mindzyx no realiza
/// diagnósticos clínicos y registro de su visualización en `dbo.auth` para auditoría.
/// Endpoints públicos (no requieren autenticación): accesibles para invitado, paciente y psicólogo.
/// </summary>
[ApiController]
[Route("api/[controller]")]
public class AvisoController(AvisoService aviso) : ControllerBase
{
    /// <summary>Devuelve el contenido del Mensaje aclaratorio (no diagnóstico clínico).</summary>
    /// <remarks>
    /// PB-04 HU1-PB4. Público. Usado por el front para cargar el texto de la modal
    /// "Mensaje aclaratorio" / "Aviso importante". Si este endpoint falla o no responde
    /// (red, back caído), el front debe mostrar "No fue posible cargar el mensaje aclaratorio. Intente nuevamente"
    /// y registrarlo con `resultado: "error"` en el log de visualización.
    ///
    /// Respuestas:
    /// - `200` — `{ "titulo": "Mensaje aclaratorio", "texto": "Mindzyx es una plataforma..." }`
    /// </remarks>
    /// <response code="200">Contenido del mensaje aclaratorio.</response>
    [HttpGet]
    [ProducesResponseType(typeof(AvisoDto), StatusCodes.Status200OK)]
    public async Task<ActionResult<AvisoDto>> Get(CancellationToken ct)
        => Ok(await aviso.GetAvisoAsync(ct));

    /// <summary>Registra la visualización del Mensaje aclaratorio (o su error de carga) en `dbo.auth`.</summary>
    /// <remarks>
    /// PB-04 HU1-PB4. Público. El front lo invoca (sin bloquear la UI) cuando se muestra la modal,
    /// ya sea por el enlace "Aviso: no diagnóstico" de la barra inferior, por el aviso automático
    /// antes de una funcionalidad sensible, o cuando la carga del contenido falló (`resultado: "error"`).
    /// Si llega un `Authorization: Bearer &lt;token&gt;`, el registro se asocia al usuario autenticado;
    /// si no, se registra como invitado/anónimo (actor nulo).
    ///
    /// Respuestas:
    /// - `200` — `{ "mensaje": "Visualización registrada" }`
    /// - `400` — `{ "error": "Este campo es obligatorio" }` (body inválido/vacío)
    /// </remarks>
    /// <response code="200">Visualización registrada en logs.</response>
    /// <response code="400">Body inválido.</response>
    [HttpPost("visualizacion")]
    [ProducesResponseType(typeof(MessageResult), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ErrorResult), StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> Visualizacion([FromBody] VisualizacionAvisoVm vm, CancellationToken ct)
    {
        if (!ModelState.IsValid) return BadRequest(new { error = "Este campo es obligatorio" });

        var ip = HttpContext.Connection.RemoteIpAddress?.ToString();
        var token = ReadOptionalBearerToken();

        await aviso.RegistrarVisualizacionAsync(token, vm.Origen, vm.Resultado, ip, ct);
        return Ok(new { mensaje = "Visualización registrada" });
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