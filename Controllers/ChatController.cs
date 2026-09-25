using Microsoft.AspNetCore.Mvc;
using Mindzyxs_API.Repositories;
using Mindzyxs_API.Services;
using Mindzyxs_API.Utilities;

namespace Mindzyxs_API.Controllers;

/// <summary>
/// Chat psicólogo-paciente (PB-31): emite el token de Firebase con el que el
/// front hace signInWithCustomToken (uid = id_usuario, claim rol). Requiere
/// sesión activa (Bearer). Las solicitudes/aceptación/bloqueo viven en SQL
/// (próximos endpoints); los mensajes en vivo van por RTDB.
/// </summary>
[ApiController]
[Route("api/[controller]")]
public class ChatController(
    FirebaseService firebase,
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

    /// <summary>Lee el token de sesión del header (requiere que sea válido).</summary>
    private string? ReadOptionalBearerToken()
    {
        if (!Request.Headers.TryGetValue("Authorization", out var v)) return null;
        var parts = v.ToString().Trim().Split(' ', StringSplitOptions.RemoveEmptyEntries);
        var token = parts.Length == 1 ? parts[0] : parts[^1];
        return token.Trim().Trim('"').Trim('\'');
    }
}
