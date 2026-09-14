using Microsoft.AspNetCore.Mvc;
using Mindzyxs_API.Dtos;
using Mindzyxs_API.Services;

namespace Mindzyxs_API.Controllers;

// API PB-01 + PB-02. Espejo del AccountController MVC viejo (Register/Verify/Resend + Login)
// pero sin vistas: JSON + códigos HTTP. HU1-PB2 / HU2-PB2 en Login + Me.
[ApiController]
[Route("api/[controller]")]
public class AccountController(
    AuthService auth,
    VerificationService verification,
    CatalogService catalogs,
    LoginService login) : ControllerBase
{
    // Catálogos para el formulario Registro (tipos Paciente/Psicologo + géneros).
    [HttpGet("catalogos")]
    public async Task<ActionResult<RegistroCatalogosDto>> Catalogos(CancellationToken ct)
        => Ok(await catalogs.GetRegistroAsync(ct));

    // PB-01 HU1+HU2+HU3. multipart/form-data (tarjeta PDF solo psicólogo).
    [HttpPost("register")]
    [Consumes("multipart/form-data")]
    public async Task<IActionResult> Register([FromForm] RegisterVm vm, CancellationToken ct)
    {
        if (!ModelState.IsValid)
        {
            var first = ModelState.Values.SelectMany(v => v.Errors).FirstOrDefault()?.ErrorMessage
                ?? "Solicitud inválida.";
            return BadRequest(new { error = first });
        }

        var r = await auth.RegisterAsync(vm, ct);
        if (!r.Success)
        {
            var status = r.Message == "El correo ya se encuentra registrado" ? 409 : 400;
            return StatusCode(status, new { error = r.Message });
        }
        return StatusCode(201, new { idUsuario = r.Data, mensaje = r.Message });
    }

    // PB-01 HU4: verificar código.
    [HttpPost("verify")]
    public async Task<IActionResult> Verify([FromBody] VerifyVm vm, CancellationToken ct)
    {
        if (!ModelState.IsValid) return BadRequest(new { error = "Este campo es obligatorio" });
        var r = await verification.VerifyAsync(vm.Correo, vm.Codigo, ct);
        if (!r.Success)
        {
            var status = r.Message.StartsWith("El código expiró") ? 410 : 400;
            return StatusCode(status, new { error = r.Message });
        }
        return Ok(new { estado = r.Data, mensaje = r.Message });
    }

    [HttpPost("resend")]
    public async Task<IActionResult> Resend([FromBody] ResendVm vm, CancellationToken ct)
    {
        if (!ModelState.IsValid) return BadRequest(new { error = "Este campo es obligatorio" });
        var r = await verification.ResendAsync(vm.Correo, ct);
        if (!r.Success) return NotFound(new { error = r.Message });
        return Ok(new { mensaje = r.Message });
    }

    // PB-02 HU1-PB2: iniciar sesión + HU2-PB2 bloqueos + logs + sesión
    [HttpPost("login")]
    public async Task<IActionResult> Login([FromBody] LoginVm vm, CancellationToken ct)
    {
        if (!ModelState.IsValid)
        {
            var first = ModelState.Values.SelectMany(v => v.Errors).FirstOrDefault()?.ErrorMessage
                ?? "Solicitud inválida.";
            return BadRequest(new { error = first });
        }

        var ip = HttpContext.Connection.RemoteIpAddress?.ToString();
        var r = await login.LoginAsync(vm.Correo, vm.Password, ip, ct);
        if (!r.Success)
        {
            // Mapeo exacto de mensajes HU
            if (r.Message == "Este campo es obligatorio" || r.Message == "Ingresa un correo válido")
                return BadRequest(new { error = r.Message });
            if (r.Message == "Correo o contraseña incorrectos")
                return Unauthorized(new { error = r.Message });
            if (r.Message == "Tu cuenta se encuentra suspendida. Contacta al administrador"
                || r.Message == "Tu cuenta está pendiente de aprobación por el administrador. Intenta más tarde")
                return StatusCode(403, new { error = r.Message });
            return BadRequest(new { error = r.Message });
        }

        return Ok(new
        {
            mensaje = r.Message,
            token = r.Data!.Token,
            expiraEn = r.Data.ExpiraEn,
            usuario = new { id = r.Data.IdUsuario, correo = r.Data.Correo, rol = r.Data.Rol, panel = r.Data.Panel, estado = r.Data.EstadoCuenta }
        });
    }

    [HttpGet("echo")]
    public IActionResult Echo() => Ok(new { auth = Request.Headers.Authorization.ToString(), allHeaders = Request.Headers.ToDictionary(h => h.Key, h => h.Value.ToString()), queryToken = Request.Query["token"].ToString() });

    // PB-02 HU2-PB2: validar sesión (expiración en segundo plano)
    [HttpGet("me")]
    public async Task<IActionResult> Me(CancellationToken ct)
    {
        // Soporta Authorization: Bearer <token> (Postman Agent, curl) y fallback ?token=
        // Postman a veces envía 'authorization' en minúsculas o con comillas
        var authHeader = "";
        if (Request.Headers.TryGetValue("Authorization", out var v1)) authHeader = v1.ToString();
        if (string.IsNullOrWhiteSpace(authHeader) && Request.Headers.TryGetValue("authorization", out var v2)) authHeader = v2.ToString();
        if (string.IsNullOrWhiteSpace(authHeader) && Request.Headers.TryGetValue("X-Auth-Token", out var v3)) authHeader = v3.ToString();

        var token = "";
        if (!string.IsNullOrWhiteSpace(authHeader))
        {
            // Permite "Bearer <token>", "<token>", "'<token>'", "\"<token>\""
            var parts = authHeader.Trim().Split(' ', StringSplitOptions.RemoveEmptyEntries);
            token = parts.Length == 1 ? parts[0] : parts[^1];
            token = token.Trim().Trim('"').Trim('\'');
        }

        // También soporta ?token=xxx para pruebas rápidas
        if (string.IsNullOrWhiteSpace(token) && Request.Query.TryGetValue("token", out var q))
            token = q.ToString().Trim().Trim('"').Trim('\'');

        var ip = HttpContext.Connection.RemoteIpAddress?.ToString();
        var r = await login.ValidateSessionAsync(token, ip, ct);
        if (!r.Success)
        {
            if (r.Message == "Tu sesión ha expirado. Inicia sesión nuevamente")
                return Unauthorized(new { error = r.Message });
            return StatusCode(403, new { error = r.Message });
        }

        return Ok(new { idUsuario = r.Data!.IdUsuario, correo = r.Data.Correo, nombre = r.Data.Nombre, apellido = r.Data.Apellido });
    }
}
