using Microsoft.AspNetCore.Mvc;
using Mindzyxs_API.Dtos;
using Mindzyxs_API.Services;

namespace Mindzyxs_API.Controllers;

/// <summary>
/// Cuenta: registro, verificación de correo, iniciar sesión, sesión activa, cierre de sesión
/// y recuperación de contraseña (PB-01, PB-02, PB-03 y PB-05).
/// Espejo JSON de la app MVC antigua, sin vistas: devuelve JSON + códigos HTTP.
/// </summary>
[ApiController]
[Route("api/[controller]")]
public class AccountController(
    AuthService auth,
    VerificationService verification,
    CatalogService catalogs,
    LoginService login,
    PasswordRecoveryService recovery,
    PerfilService perfil) : ControllerBase
{
    /// <summary>Obtiene los catálogos del formulario de registro (tipos de cuenta y géneros).</summary>
    /// <remarks>
    /// Usado por la pantalla Registro para llenar el ComboBox "Tipo de usuario"
    /// (solo `Paciente`/`Psicologo`) y el select "Género".
    /// No requiere autenticación.
    /// </remarks>
    /// <response code="200">Lista de tipos de cuenta y géneros.</response>
    [HttpGet("catalogos")]
    [ProducesResponseType(typeof(RegistroCatalogosDto), StatusCodes.Status200OK)]
    public async Task<ActionResult<RegistroCatalogosDto>> Catalogos(CancellationToken ct)
        => Ok(await catalogs.GetRegistroAsync(ct));

    /// <summary>Registra un nuevo usuario (Paciente o Psicólogo).</summary>
    /// <remarks>
    /// PB-01 HU1 + HU2 + HU3. Cuerpo `multipart/form-data`.
    /// El campo `TarjetaProfesional` (PDF ≤5MB) es obligatorio solo para Psicólogo.
    /// `FechaNacimiento` va en formato `yyyy-MM-dd`, no puede ser una fecha futura
    /// ni indicar una edad menor a 14 años.
    /// Crea la cuenta en estado `PendienteVerificacion` (`PendienteAprobacion` para Psicólogo),
    /// genera un código de 6 dígitos con expiración de 15 min y lo envía por correo.
    ///
    /// Respuestas:
    /// - `201` — `{ "idUsuario": 42, "mensaje": "Registro exitoso. Verifica tu cuenta." }`
    /// - `400` — `{ "error": "..." }` (validación de HU o tarjeta inválida)
    /// - `409` — `{ "error": "El correo ya se encuentra registrado" }`
    /// </remarks>
    /// <response code="201">Usuario creado; espera verificación de correo.</response>
    /// <response code="400">Datos inválidos u obligatorios faltantes.</response>
    /// <response code="409">El correo ya está registrado.</response>
    [HttpPost("register")]
    [Consumes("multipart/form-data")]
    [ProducesResponseType(typeof(RegisterResult), StatusCodes.Status201Created)]
    [ProducesResponseType(typeof(ErrorResult), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ErrorResult), StatusCodes.Status409Conflict)]
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

    /// <summary>Verifica el código de 6 dígitos enviado al correo durante el registro.</summary>
    /// <remarks>
    /// PB-01 HU4. Si el código es correcto, la cuenta pasa a `Activa` (Paciente)
    /// o `PendienteAprobacion` (Psicólogo, esperando aprobación del administrador).
    ///
    /// Respuestas:
    /// - `200` — `{ "estado": "Activa", "mensaje": "Registro exitoso. Ya puedes iniciar sesión" }`
    /// - `200` — `{ "estado": "PendienteAprobacion", "mensaje": "Registro exitoso. Tu cuenta está pendiente de aprobación por el administrador" }` (Psicólogo)
    /// - `400` — `{ "error": "Código incorrecto. Verifica e intenta nuevamente" }`
    /// - `410` — `{ "error": "El código expiró. Reenvía el código" }`
    /// </remarks>
    /// <response code="200">Código válido; la cuenta queda verificada.</response>
    /// <response code="400">Código incorrecto o datos inválidos.</response>
    /// <response code="410">El código expiró; reenviar.</response>
    [HttpPost("verify")]
    [ProducesResponseType(typeof(VerifyResult), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ErrorResult), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ErrorResult), StatusCodes.Status410Gone)]
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

    /// <summary>Reenvía el código de verificación del registro al correo.</summary>
    /// <remarks>
    /// PB-01 HU4. Devuelve `200` aunque el correo no exista, para no revelar qué cuentas están registradas.
    /// Respuestas:
    /// - `200` — `{ "mensaje": "Código reenviado" }`
    /// - `400` — `{ "error": "Ingresa un correo válido" }` / `{ "error": "Este campo es obligatorio" }`
    /// - `404` — `{ "error": "No fue posible reenviar el código" }`
    /// </remarks>
    /// <response code="200">Código reenviado.</response>
    /// <response code="400">Correo inválido u obligatorio.</response>
    /// <response code="404">Cuenta en estado que no permite reenvío.</response>
    [HttpPost("resend")]
    [ProducesResponseType(typeof(MessageResult), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ErrorResult), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ErrorResult), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Resend([FromBody] ResendVm vm, CancellationToken ct)
    {
        if (!ModelState.IsValid) return BadRequest(new { error = "Este campo es obligatorio" });
        var r = await verification.ResendAsync(vm.Correo, ct);
        if (!r.Success) return NotFound(new { error = r.Message });
        return Ok(new { mensaje = r.Message });
    }

    /// <summary>Inicia sesión con correo y contraseña.</summary>
    /// <remarks>
    /// PB-02 HU1-PB2 + HU2-PB2 (bloqueos por estado, log e historial). Verifica la contraseña
    /// (hash PBKDF2), crea una sesión de 8 horas y registra el intento en `dbo.auth`.
    ///
    /// Respuestas:
    /// - `200` — token de sesión + datos del usuario:
    ///   `{ "mensaje": "Inicio de sesión exitoso", "token": "...", "expiraEn": "...",
    ///      "usuario": { "id", "correo", "rol", "panel", "estado" } }`
    /// - `400` — `{ "error": "Este campo es obligatorio" }` / `{ "error": "Ingresa un correo válido" }`
    /// - `401` — `{ "error": "Correo o contraseña incorrectos" }` (genérico)
    /// - `403` — `{ "error": "Tu cuenta se encuentra suspendida. Contacta al administrador" }`
    /// - `403` — `{ "error": "Tu cuenta está pendiente de aprobación por el administrador. Intenta más tarde" }`
    /// </remarks>
    /// <response code="200">Login exitoso; devuelve token y perfil.</response>
    /// <response code="400">Datos de entrada inválidos.</response>
    /// <response code="401">Credenciales incorrectas.</response>
    /// <response code="403">Cuenta suspendida o pendiente de aprobación.</response>
    [HttpPost("login")]
    [ProducesResponseType(typeof(LoginResult), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ErrorResult), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ErrorResult), StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(typeof(ErrorResult), StatusCodes.Status403Forbidden)]
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

    /// <summary>Eco de depuración del header Authorization recibido (no funcional en producción).</summary>
    /// <remarks>
    /// Devuelve el valor de `Authorization`, todos los headers y el token vía `?token=` para
    /// diagnosticar problemas de autenticación en Postman/curl.
    /// </remarks>
    /// <response code="200">Información de depuración del request.</response>
    [HttpGet("echo")]
    [ProducesResponseType(typeof(object), StatusCodes.Status200OK)]
    public IActionResult Echo() => Ok(new { auth = Request.Headers.Authorization.ToString(), allHeaders = Request.Headers.ToDictionary(h => h.Key, h => h.Value.ToString()), queryToken = Request.Query["token"].ToString() });

    /// <summary>Valida la sesión activa del token de sesión.</summary>
    /// <remarks>
    /// PB-02 HU2-PB2. Requiere `Authorization: Bearer &lt;token&gt;`.
    /// Verifica que la sesión exista y no haya expirado (expira en 8h),
    /// actualiza el último acceso y loguea la consulta en `dbo.auth`.
    ///
    /// Respuestas:
    /// - `200` — `{ "idUsuario": 42, "correo": "...", "nombre": "...", "apellido": "..." }`
    /// - `401` — `{ "error": "Tu sesión ha expirado. Inicia sesión nuevamente" }`
    /// - `403` — `{ "error": "Tu cuenta se encuentra suspendida. Contacta al administrador" }`
    /// </remarks>
    /// <response code="200">Sesión válida; datos del usuario autenticado.</response>
    /// <response code="401">Token ausente, inválido o expirado.</response>
    /// <response code="403">Cuenta suspendida.</response>
    [HttpGet("me")]
    [ProducesResponseType(typeof(MeResult), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ErrorResult), StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(typeof(ErrorResult), StatusCodes.Status403Forbidden)]
    public async Task<IActionResult> Me(CancellationToken ct)
    {
        var token = ReadBearerToken();
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

    /// <summary>Cierra la sesión actual: invalida el token y registra el evento.</summary>
    /// <remarks>
    /// PB-03 HU1-PB3. Requiere `Authorization: Bearer &lt;token&gt;`.
    /// Borra la sesión de `dbo.sesiones` (token inválido de inmediato) y registra `LOGOUT` en
    /// `dbo.auth`. Es idempotente: sin token o sesión inexistente devuelve `200` para que el
    /// front limpie el token local. Un error interno devuelve `500` y la sesión sigue activa.
    /// </remarks>
    /// <response code="200">Sesión cerrada; el token ya no es válido.</response>
    /// <response code="500">No fue posible cerrar sesión.</response>
    [HttpPost("logout")]
    [ProducesResponseType(typeof(MessageResult), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ErrorResult), StatusCodes.Status500InternalServerError)]
    public async Task<IActionResult> Logout(CancellationToken ct)
    {
        var token = ReadBearerToken();
        var ip = HttpContext.Connection.RemoteIpAddress?.ToString();
        var r = await login.LogoutAsync(token, ip, ct);
        if (!r.Success)
            return StatusCode(500, new { error = r.Message });
        return Ok(new { mensaje = r.Message });
    }

    /// <summary>Lee el token de sesión de forma robusta: Authorization Bearer &lt;token&gt;, sin prefijo, con comillas o minúsculas, o `?token=` para pruebas.</summary>
    private string? ReadBearerToken()
    {
        var authHeader = "";
        if (Request.Headers.TryGetValue("Authorization", out var v1)) authHeader = v1.ToString();
        if (string.IsNullOrWhiteSpace(authHeader) && Request.Headers.TryGetValue("authorization", out var v2)) authHeader = v2.ToString();
        if (string.IsNullOrWhiteSpace(authHeader) && Request.Headers.TryGetValue("X-Auth-Token", out var v3)) authHeader = v3.ToString();

        if (!string.IsNullOrWhiteSpace(authHeader))
        {
            // Permite "Bearer <token>", "<token>", "'<token>'", "\"<token>\""
            var parts = authHeader.Trim().Split(' ', StringSplitOptions.RemoveEmptyEntries);
            var token = parts.Length == 1 ? parts[0] : parts[^1];
            return token.Trim().Trim('"').Trim('\'');
        }

        // También soporta ?token=xxx para pruebas rápidas
        if (Request.Query.TryGetValue("token", out var q))
            return q.ToString().Trim().Trim('"').Trim('\'');

        return null;
    }

    /// <summary>Solicita un código de recuperación de contraseña.</summary>
    /// <remarks>
    /// PB-05 HU1-PB5. Envía por correo un código de 6 dígitos (tipo `Recuperar`, expira en 15 min).
    /// Si el correo NO está registrado devuelve `200` con mensaje genérico
    /// (no confirma la existencia de cuentas).
    /// Respuestas:
    /// - `200` — `{ "mensaje": "Código enviado a tu correo" }` (correo registrado)
    /// - `200` — `{ "mensaje": "Si el correo está registrado, recibirás un código" }` (correo inexistente)
    /// - `400` — `{ "error": "Este campo es obligatorio" }` / `{ "error": "Ingresa un correo válido" }`
    /// </remarks>
    /// <response code="200">Código enviado (o respuesta genérica por seguridad).</response>
    /// <response code="400">Correo inválido u obligatorio.</response>
    [HttpPost("forgot")]
    [ProducesResponseType(typeof(MessageResult), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ErrorResult), StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> Forgot([FromBody] ForgotVm vm, CancellationToken ct)
    {
        if (!ModelState.IsValid)
        {
            var first = ModelState.Values.SelectMany(v => v.Errors).FirstOrDefault()?.ErrorMessage
                ?? "Solicitud inválida.";
            return BadRequest(new { error = first });
        }

        var r = await recovery.ForgotAsync(vm.Correo, ct);
        if (!r.Success) return BadRequest(new { error = r.Message });
        return Ok(new { mensaje = r.Message });
    }

    /// <summary>Valida el código recibido antes de permitir definir la nueva contraseña.</summary>
    /// <remarks>
    /// PB-05 HU2-PB5. Respuestas:
    /// - `200` — `{ "mensaje": "Código verificado. Define tu nueva contraseña" }`
    /// - `400` — `{ "error": "Código incorrecto. Verifica e intenta nuevamente" }`
    /// - `410` — `{ "error": "El código expiró. Reenvía el código" }`
    /// </remarks>
    /// <response code="200">Código correcto; permite continuar a la nueva contraseña.</response>
    /// <response code="400">Código incorrecto o datos inválidos.</response>
    /// <response code="410">El código expiró; reenviar.</response>
    [HttpPost("forgot/verify")]
    [ProducesResponseType(typeof(MessageResult), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ErrorResult), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ErrorResult), StatusCodes.Status410Gone)]
    public async Task<IActionResult> VerifyCode([FromBody] VerifyCodeVm vm, CancellationToken ct)
    {
        if (!ModelState.IsValid) return BadRequest(new { error = "Este campo es obligatorio" });
        var r = await recovery.VerifyCodeAsync(vm.Correo, vm.Codigo, ct);
        if (!r.Success)
        {
            var status = r.Message.StartsWith("El código expiró") ? 410 : 400;
            return StatusCode(status, new { error = r.Message });
        }
        return Ok(new { mensaje = r.Message });
    }

    /// <summary>Guarda la nueva contraseña tras verificar el código de recuperación.</summary>
    /// <remarks>
    /// PB-05 HU2-PB5. Respuestas:
    /// - `200` — `{ "mensaje": "Contraseña actualizada exitosamente" }`
    /// - `400` — `{ "error": "Código incorrecto. Verifica e intenta nuevamente" }`
    /// - `400` — `{ "error": "La contraseña debe tener mínimo 6 caracteres e incluir al menos una letra y un número" }`
    /// - `410` — `{ "error": "El código expiró. Reenvía el código" }`
    /// </remarks>
    /// <response code="200">Contraseña actualizada.</response>
    /// <response code="400">Código incorrecto, contraseña débil o datos inválidos.</response>
    /// <response code="410">El código expiró; reenviar.</response>
    [HttpPost("forgot/reset")]
    [ProducesResponseType(typeof(MessageResult), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ErrorResult), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ErrorResult), StatusCodes.Status410Gone)]
    public async Task<IActionResult> ResetPassword([FromBody] ResetPasswordVm vm, CancellationToken ct)
    {
        if (!ModelState.IsValid) return BadRequest(new { error = "Este campo es obligatorio" });
        var ip = HttpContext.Connection.RemoteIpAddress?.ToString();
        var r = await recovery.ResetPasswordAsync(vm.Correo, vm.Codigo, vm.Password, ip, ct);
        if (!r.Success)
        {
            var status = r.Message.StartsWith("El código expiró") ? 410 : 400;
            return StatusCode(status, new { error = r.Message });
        }
        return Ok(new { mensaje = r.Message });
    }

    /// <summary>Reenvía el código de recuperación de contraseña.</summary>
    /// <remarks>
    /// PB-05 HU2-PB5. Devuelve `200` genérico aunque el correo no exista, por seguridad.
    /// Respuestas:
    /// - `200` — `{ "mensaje": "Código reenviado" }`
    /// - `400` — `{ "error": "Este campo es obligatorio" }` / `{ "error": "Ingresa un correo válido" }`
    /// </remarks>
    /// <response code="200">Código reenviado (respuesta genérica por seguridad).</response>
    /// <response code="400">Correo inválido u obligatorio.</response>
    [HttpPost("forgot/resend")]
    [ProducesResponseType(typeof(MessageResult), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ErrorResult), StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> ResendRecovery([FromBody] ForgotVm vm, CancellationToken ct)
    {
        if (!ModelState.IsValid) return BadRequest(new { error = "Este campo es obligatorio" });
        var r = await recovery.ResendAsync(vm.Correo, ct);
        if (!r.Success) return BadRequest(new { error = r.Message });
        return Ok(new { mensaje = r.Message });
    }

    /// <summary>Actualiza nombres, apellidos, fecha de nacimiento y género.</summary>
    /// <remarks>
    /// PB-10 HU1-PB10. Requiere sesión. Valida obligatorios, fecha válida
    /// (no futura, mayor de 14) y género existente. Guarda `PERFIL_ACTUALIZADO`.
    ///
    /// Respuestas:
    /// - `200` — `{ "mensaje": "Perfil actualizado correctamente" }`
    /// - `400` — `{ "error": "Este campo es obligatorio" }` / `Ingresa una fecha válida` / `Ingresa una edad válida` / `Selecciona un género válido`
    /// - `401` — sin sesión.
    /// </remarks>
    /// <response code="200">Perfil actualizado.</response>
    /// <response code="400">Validación.</response>
    /// <response code="401">Sesión requerida.</response>
    [HttpPut("perfil")]
    [ProducesResponseType(typeof(MessageResult), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ErrorResult), StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> ActualizarPerfil([FromBody] ActualizarPerfilVm vm, CancellationToken ct)
    {
        if (!ModelState.IsValid || vm is null)
            return BadRequest(new { error = "Este campo es obligatorio" });
        try
        {
            var ip = HttpContext.Connection.RemoteIpAddress?.ToString();
            var mensaje = await perfil.ActualizarAsync(ReadBearerToken(), vm, ip, ct);
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

    /// <summary>Envía código de verificación al nuevo correo.</summary>
    /// <remarks>
    /// PB-10 HU1-PB10. Requiere sesión. Si el correo ya existe → 409.
    ///
    /// Respuestas:
    /// - `200` — `{ "mensaje": "Código enviado" }`
    /// - `400` — `{ "error": "Ingresa un correo válido" }`
    /// - `401` — sin sesión.
    /// - `409` — `{ "error": "El correo ya se encuentra registrado" }`
    /// </remarks>
    /// <response code="200">Código enviado al nuevo correo.</response>
    /// <response code="400">Correo inválido.</response>
    /// <response code="401">Sesión requerida.</response>
    /// <response code="409">Correo duplicado.</response>
    [HttpPost("correo/solicitar")]
    [ProducesResponseType(typeof(MessageResult), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ErrorResult), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ErrorResult), StatusCodes.Status409Conflict)]
    public async Task<IActionResult> SolicitarCambioCorreo([FromBody] SolicitarCambioCorreoVm vm, CancellationToken ct)
    {
        if (!ModelState.IsValid || vm is null)
            return BadRequest(new { error = "Este campo es obligatorio" });
        try
        {
            var ip = HttpContext.Connection.RemoteIpAddress?.ToString();
            var mensaje = await perfil.SolicitarCambioAsync(ReadBearerToken(), vm.NuevoCorreo, ip, ct);
            return Ok(new { mensaje });
        }
        catch (UnauthorizedAccessException ex)
        {
            return Unauthorized(new { error = ex.Message });
        }
        catch (InvalidOperationException ex) when (ex.Message == "El correo ya se encuentra registrado")
        {
            return Conflict(new { error = ex.Message });
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(new { error = ex.Message });
        }
    }

    /// <summary>Valida el código y aplica el cambio de correo.</summary>
    /// <remarks>
    /// PB-10 HU1-PB10. Requiere sesión. Guarda `CORREO_ACTUALIZADO`.
    ///
    /// Respuestas:
    /// - `200` — `{ "mensaje": "Correo actualizado correctamente" }`
    /// - `400` — código incorrecto o datos inválidos.
    /// - `401` — sin sesión.
    /// - `409` — correo tomado entretanto.
    /// - `410` — `{ "error": "El código expiró. Reenvía el código" }`
    /// </remarks>
    /// <response code="200">Correo actualizado.</response>
    /// <response code="400">Código incorrecto o datos inválidos.</response>
    /// <response code="401">Sesión requerida.</response>
    /// <response code="409">Correo duplicado.</response>
    /// <response code="410">Código expirado.</response>
    [HttpPost("correo/confirmar")]
    [ProducesResponseType(typeof(MessageResult), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ErrorResult), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ErrorResult), StatusCodes.Status410Gone)]
    [ProducesResponseType(typeof(ErrorResult), StatusCodes.Status409Conflict)]
    public async Task<IActionResult> ConfirmarCambioCorreo([FromBody] ConfirmarCambioCorreoVm vm, CancellationToken ct)
    {
        if (!ModelState.IsValid || vm is null)
            return BadRequest(new { error = "Este campo es obligatorio" });
        try
        {
            var ip = HttpContext.Connection.RemoteIpAddress?.ToString();
            var mensaje = await perfil.ConfirmarCambioAsync(ReadBearerToken(), vm.NuevoCorreo, vm.Codigo, ip, ct);
            return Ok(new { mensaje });
        }
        catch (UnauthorizedAccessException ex)
        {
            return Unauthorized(new { error = ex.Message });
        }
        catch (InvalidOperationException ex) when (ex.Message == "El correo ya se encuentra registrado")
        {
            return Conflict(new { error = ex.Message });
        }
        catch (InvalidOperationException ex)
        {
            var status = ex.Message.StartsWith("El código expiró") ? 410 : 400;
            return StatusCode(status, new { error = ex.Message });
        }
    }

    /// <summary>Reanuda una verificación inconclusa (registro sin verificar).</summary>
    /// <remarks>
    /// Si el correo sigue en `PendienteVerificacion`, reenvía código y avisa para
    /// continuar; si no, mensaje genérico (sin revelar estados).
    ///
    /// Respuestas:
    /// - `200` — `{ "mensaje": ..., "requiereVerificacion": true|false }`
    /// - `400` — correo inválido.
    /// </remarks>
    /// <response code="200">Estado de verificación.</response>
    /// <response code="400">Correo inválido.</response>
    [HttpPost("verificacion/reanudar")]
    [ProducesResponseType(typeof(MessageResult), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ErrorResult), StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> ReanudarVerificacion([FromBody] ReanudarVerificacionVm vm, CancellationToken ct)
    {
        if (!ModelState.IsValid || vm is null || string.IsNullOrWhiteSpace(vm.Correo))
            return BadRequest(new { error = "Ingresa un correo válido" });
        var (requiere, mensaje) = await perfil.ReanudarAsync(vm.Correo, ct);
        return Ok(new { mensaje, requiereVerificacion = requiere });
    }
}