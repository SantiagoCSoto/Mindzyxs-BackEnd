using Microsoft.AspNetCore.Mvc;
using Mindzyxs_API.Dtos;
using Mindzyxs_API.Repositories;
using Mindzyxs_API.Services;
using Mindzyxs_API.Utilities;

namespace Mindzyxs_API.Controllers;

/// <summary>
/// Chat psicólogo-paciente (PB-31): token Firebase + ciclo de vida en SQL
/// (solicitudes, aceptación/rechazo, bloqueo). Los mensajes en vivo viajan
/// por RTDB; aquí se validan roles, estados y se audita en dbo.auth.
/// </summary>
[ApiController]
[Route("api/[controller]")]
public class ChatController(
    FirebaseService firebase,
    ChatService chats,
    ISesionRepository sesiones,
    IUserRepository usuarios) : ControllerBase
{
    /// <summary>Emite el token de Firebase para el chat realtime.</summary>
    /// <remarks>
    /// PB-31. Requiere `Authorization: Bearer &lt;token&gt;` de sesión activa.
    /// Devuelve `{ "firebaseToken": ..., "uid": "14", "rol": "Paciente",
    /// "databaseUrl": "https://..." }` (expira en 1h; pedir otro al vencer).
    ///
    /// Respuestas:
    /// - `200` — token emitido.
    /// - `401` — sin sesión o sesión expirada.
    /// </remarks>
    /// <response code="200">Token de Firebase emitido.</response>
    /// <response code="401">Sesión requerida.</response>
    [HttpPost("token")]
    public async Task<IActionResult> Token(CancellationToken ct)
    {
        var raw = ReadOptionalBearerToken();
        if (string.IsNullOrWhiteSpace(raw)) return Unauthorized(new { error = "Sesión requerida" });

        var sesion = await sesiones.FindByTokenHashAsync(TokenHelper.Hash(raw.Trim()), ct);
        if (sesion is null || sesion.ExpiraEn < DateTime.UtcNow)
            return Unauthorized(new { error = "Sesión requerida" });

        var user = await usuarios.FindByIdAsync(sesion.IdUsuario, ct);
        if (user is null) return Unauthorized(new { error = "Sesión requerida" });

        var rol = await usuarios.GetRolNombreAsync(user.IdRol, ct) ?? "Paciente";
        var opt = HttpContext.RequestServices
            .GetRequiredService<Microsoft.Extensions.Options.IOptions<FirebaseOptions>>().Value;
        var token = await firebase.CreateCustomTokenAsync(user.IdUsuario.ToString(), rol, ct);
        return Ok(new
        {
            firebaseToken = token,
            uid = user.IdUsuario.ToString(),
            rol,
            databaseUrl = opt.DatabaseUrl
        });
    }

    /// <summary>Mis conversaciones (solicitudes incluidas).</summary>
    /// <remarks>
    /// PB-31. Requiere sesión. El paciente ve las suyas (para aceptar/rechazar
    /// pendientes); el psicólogo las suyas. Notificación interna = pendientes aquí.
    ///
    /// Respuestas: `200` — `[{ "id": ..., "estado": ..., "bloqueadaPorMi": ..., "contraparte": {...}, "fechaCreacion": ... }]`; `401` sin sesión.
    /// </remarks>
    /// <response code="200">Conversaciones propias.</response>
    /// <response code="401">Sesión requerida.</response>
    [HttpGet("conversaciones")]
    [ProducesResponseType(typeof(List<ConversacionDto>), StatusCodes.Status200OK)]
    public async Task<IActionResult> MisConversaciones(CancellationToken ct)
    {
        try
        {
            var ip = HttpContext.Connection.RemoteIpAddress?.ToString();
            return Ok(await chats.MisConversacionesAsync(ReadOptionalBearerToken(), ip, ct));
        }
        catch (UnauthorizedAccessException ex)
        {
            return Unauthorized(new { error = ex.Message });
        }
    }

    /// <summary>El psicólogo ofrece chat a un paciente (solicitud Pendiente).</summary>
    /// <remarks>
    /// PB-31. Solo psicólogo aprobado y activo; paciente existente, activo y con
    /// rol Paciente. Si hubo Rechazada/Cerrada se reactiva. Guarda `CHAT_SOLICITUD`.
    ///
    /// Respuestas: `200` — conversación; `400` — validación; `401` sin sesión.
    /// </remarks>
    /// <response code="200">Solicitud creada/reactivada.</response>
    /// <response code="400">Validación.</response>
    /// <response code="401">Sesión requerida.</response>
    [HttpPost("solicitudes")]
    [ProducesResponseType(typeof(ConversacionDto), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ErrorResult), StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> Solicitar([FromBody] SolicitudChatVm vm, CancellationToken ct)
    {
        if (!ModelState.IsValid || vm.IdPaciente <= 0)
            return BadRequest(new { error = "Este campo es obligatorio" });
        try
        {
            var ip = HttpContext.Connection.RemoteIpAddress?.ToString();
            return Ok(await chats.CrearSolicitudAsync(ReadOptionalBearerToken(), vm.IdPaciente, ip, ct));
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

    /// <summary>El paciente acepta su solicitud (escribe meta Aceptada en RTDB).</summary>
    /// <remarks>PB-31. Solo el paciente dueño y en estado Pendiente. Guarda `CHAT_ACEPTADO`.</remarks>
    /// <response code="200">Conversación aceptada.</response>
    /// <response code="400">Validación.</response>
    /// <response code="401">Sesión requerida.</response>
    /// <response code="404">No existe.</response>
    [HttpPost("solicitudes/{id:int}/aceptar")]
    [ProducesResponseType(typeof(ConversacionDto), StatusCodes.Status200OK)]
    public async Task<IActionResult> Aceptar([FromRoute] int id, CancellationToken ct)
        => await Responder(id, true, ct);

    /// <summary>El paciente rechaza su solicitud.</summary>
    /// <remarks>PB-31. Solo el paciente dueño y en estado Pendiente. Guarda `CHAT_RECHAZADO`.</remarks>
    /// <response code="200">Solicitud rechazada.</response>
    /// <response code="400">Validación.</response>
    /// <response code="401">Sesión requerida.</response>
    /// <response code="404">No existe.</response>
    [HttpPost("solicitudes/{id:int}/rechazar")]
    [ProducesResponseType(typeof(ConversacionDto), StatusCodes.Status200OK)]
    public async Task<IActionResult> Rechazar([FromRoute] int id, CancellationToken ct)
        => await Responder(id, false, ct);

    /// <summary>El paciente bloquea el chat (escribe meta Bloqueada en RTDB).</summary>
    /// <remarks>
    /// PB-31. Solo en Aceptada. El bloqueado que intente escribir recibe aviso
    /// (reglas RTDB + 403 aquí). Guarda `CHAT_BLOQUEO`.
    /// </remarks>
    /// <response code="200">Chat bloqueado.</response>
    /// <response code="400">Validación.</response>
    /// <response code="401">Sesión requerida.</response>
    /// <response code="404">No existe.</response>
    [HttpPost("conversaciones/{id:int}/bloqueo")]
    [ProducesResponseType(typeof(ConversacionDto), StatusCodes.Status200OK)]
    public async Task<IActionResult> Bloquear([FromRoute] int id, CancellationToken ct)
    {
        try
        {
            var ip = HttpContext.Connection.RemoteIpAddress?.ToString();
            return Ok(await chats.BloquearAsync(ReadOptionalBearerToken(), id, ip, ct));
        }
        catch (UnauthorizedAccessException ex)
        {
            return Unauthorized(new { error = ex.Message });
        }
        catch (InvalidOperationException ex) when (ex.Message == "Conversación no encontrada.")
        {
            return NotFound(new { error = ex.Message });
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(new { error = ex.Message });
        }
    }

    /// <summary>El paciente desbloquea (vuelve a Aceptada, meta actualizada).</summary>
    /// <remarks>PB-31. Solo quien bloqueó. Guarda `CHAT_DESBLOQUEO`.</remarks>
    /// <response code="200">Chat desbloqueado.</response>
    /// <response code="400">Validación.</response>
    /// <response code="401">Sesión requerida.</response>
    /// <response code="404">No existe.</response>
    [HttpDelete("conversaciones/{id:int}/bloqueo")]
    [ProducesResponseType(typeof(ConversacionDto), StatusCodes.Status200OK)]
    public async Task<IActionResult> Desbloquear([FromRoute] int id, CancellationToken ct)
    {
        try
        {
            var ip = HttpContext.Connection.RemoteIpAddress?.ToString();
            return Ok(await chats.DesbloquearAsync(ReadOptionalBearerToken(), id, ip, ct));
        }
        catch (UnauthorizedAccessException ex)
        {
            return Unauthorized(new { error = ex.Message });
        }
        catch (InvalidOperationException ex) when (ex.Message == "Conversación no encontrada.")
        {
            return NotFound(new { error = ex.Message });
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(new { error = ex.Message });
        }
    }

    /// <summary>Aviso best-effort de mensaje enviado (el texto viaja por RTDB).</summary>
    /// <remarks>
    /// PB-31. El front lo llama sin bloquear tras el `push()` para dejar
    /// auditoría `CHAT_MENSAJE` en SQL. Bloqueado que no bloqueó → 403 + aviso.
    /// </remarks>
    /// <response code="200">Aviso registrado.</response>
    /// <response code="400">Validación.</response>
    /// <response code="401">Sesión requerida.</response>
    /// <response code="403">Bloqueado.</response>
    /// <response code="404">No existe.</response>
    [HttpPost("conversaciones/{id:int}/mensajes/aviso")]
    public async Task<IActionResult> AvisarMensaje([FromRoute] int id, CancellationToken ct)
    {
        try
        {
            var ip = HttpContext.Connection.RemoteIpAddress?.ToString();
            await chats.AvisarMensajeAsync(ReadOptionalBearerToken(), id, ip, ct);
            return Ok(new { mensaje = "Aviso registrado" });
        }
        catch (UnauthorizedAccessException ex)
        {
            return Unauthorized(new { error = ex.Message });
        }
        catch (InvalidOperationException ex) when (ex.Message == "Conversación no encontrada.")
        {
            return NotFound(new { error = ex.Message });
        }
        catch (InvalidOperationException ex) when (ex.Message == "Has sido bloqueado por este usuario.")
        {
            return StatusCode(StatusCodes.Status403Forbidden, new { error = ex.Message });
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(new { error = ex.Message });
        }
    }

    /// <summary>Buscador multifiltro de pacientes (nombre opcional, exige un filtro).</summary>
    /// <remarks>
    /// PB-31. Solo psicólogo aprobado y activo. Filtros combinables: `q` (nombre/
    /// apellido/correo, opcional), `nivel` (Bajo/Medio/Alto), `minCuestionarios`
    /// (cuestionarios respondidos) y `tienePsicologo` (true/false). Sin ningún
    /// filtro → 400 "Seleccione al menos un filtro antes de buscar."
    /// Cada uno trae último riesgo + conteos para decidir a quién ofrecerle chat.
    ///
    /// Respuestas: `200` — lista; `400` sin filtros; `401` sin sesión; `403` sin rol.
    /// </remarks>
    /// <response code="200">Pacientes encontrados.</response>
    /// <response code="400">Sin filtros.</response>
    /// <response code="401">Sesión requerida.</response>
    /// <response code="403">Solo psicólogos.</response>
    [HttpGet("pacientes")]
    [ProducesResponseType(typeof(List<PacienteChatDto>), StatusCodes.Status200OK)]
    public async Task<IActionResult> BuscarPacientes(
        [FromQuery] string? q,
        [FromQuery] string? nivel,
        [FromQuery] int? minCuestionarios,
        [FromQuery] bool? tienePsicologo,
        CancellationToken ct)
    {
        try
        {
            return Ok(await chats.BuscarPacientesAsync(ReadOptionalBearerToken(), q, nivel, minCuestionarios, tienePsicologo, ct));
        }
        catch (UnauthorizedAccessException ex)
        {
            return Unauthorized(new { error = ex.Message });
        }
        catch (InvalidOperationException ex) when (ex.Message.StartsWith("Se requiere rol"))
        {
            return StatusCode(StatusCodes.Status403Forbidden, new { error = ex.Message });
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(new { error = ex.Message });
        }
    }

    private async Task<IActionResult> Responder(int id, bool aceptar, CancellationToken ct)
    {
        try
        {
            var ip = HttpContext.Connection.RemoteIpAddress?.ToString();
            return Ok(await chats.ResponderAsync(ReadOptionalBearerToken(), id, aceptar, ip, ct));
        }
        catch (UnauthorizedAccessException ex)
        {
            return Unauthorized(new { error = ex.Message });
        }
        catch (InvalidOperationException ex) when (ex.Message == "Conversación no encontrada.")
        {
            return NotFound(new { error = ex.Message });
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(new { error = ex.Message });
        }
    }

    /// <summary>Lee el token de sesión del header (requiere que sea válido).</summary>
    private string? ReadOptionalBearerToken()
    {
        if (!Request.Headers.TryGetValue("Authorization", out var v)) return null;
        var parts = v.ToString().Trim().Split(' ', StringSplitOptions.RemoveEmptyEntries);
        var token = parts.Length == 1 ? parts[0] : parts[^1];
        return token.Trim().Trim('"').Trim('\'');
    }
}
