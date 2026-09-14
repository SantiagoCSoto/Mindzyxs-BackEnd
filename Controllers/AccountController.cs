using Microsoft.AspNetCore.Mvc;
using Mindzyxs_API.Dtos;
using Mindzyxs_API.Services;

namespace Mindzyxs_API.Controllers;

// API PB-01. Estructura espejo del AccountController MVC viejo (Register/Verify/Resend),
// pero sin vistas: JSON + códigos HTTP.
[ApiController]
[Route("api/[controller]")]
public class AccountController(
    AuthService auth,
    VerificationService verification,
    CatalogService catalogs) : ControllerBase
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
}
