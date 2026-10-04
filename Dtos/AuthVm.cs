using System.ComponentModel.DataAnnotations;

namespace Mindzyxs_API.Dtos;

/// <summary>
/// PB-01: registro (HU1 tipo cuenta + HU2 datos + HU3 tarjeta). Se envía como `multipart/form-data`.
/// </summary>
public class RegisterVm
{
    /// <summary>Tipo de cuenta: `Paciente` o `Psicologo`.</summary>
    [Required(ErrorMessage = "Debes seleccionar un tipo de cuenta: Paciente o Psicólogo")]
    public string? TipoUsuario { get; set; }

    /// <summary>Nombre(s) del usuario.</summary>
    [Required(ErrorMessage = "Este campo es obligatorio")]
    public string? Nombre { get; set; }

    /// <summary>Apellido(s) del usuario.</summary>
    [Required(ErrorMessage = "Este campo es obligatorio")]
    public string? Apellido { get; set; }

    /// <summary>
    /// Fecha de nacimiento en formato `yyyy-MM-dd` (string para validación manual del backend).
    /// No puede ser una fecha futura ni indicar una edad menor a 14 años.
    /// </summary>
    [Required(ErrorMessage = "Este campo es obligatorio")]
    public string? FechaNacimiento { get; set; }

    /// <summary>Id del género, tomado de `GET /api/account/catalogos`.</summary>
    [Required(ErrorMessage = "Este campo es obligatorio")]
    public int? IdGenero { get; set; }

    /// <summary>Correo electrónico del usuario (único).</summary>
    [Required(ErrorMessage = "Este campo es obligatorio")]
    [EmailAddress(ErrorMessage = "Ingresa un correo válido")]
    public string? Correo { get; set; }

    /// <summary>Contraseña: mínimo 6 caracteres, al menos una letra y un número.</summary>
    [Required(ErrorMessage = "Este campo es obligatorio")]
    public string? Password { get; set; }

    /// <summary>
    /// Tarjeta profesional del psicólogo (PDF ≤5MB). Solo obligatoria para `Psicologo` (HU3-PB1);
    /// el Paciente lo envía null.
    /// </summary>
    public IFormFile? TarjetaProfesional { get; set; }
}

/// <summary>Cuerpo de <c>POST /api/account/verify</c> (verificación de código del registro).</summary>
public class VerifyVm
{
    /// <summary>Correo del usuario registrado.</summary>
    [Required(ErrorMessage = "Este campo es obligatorio")]
    [EmailAddress(ErrorMessage = "Ingresa un correo válido")]
    public string? Correo { get; set; }

    /// <summary>Código de verificación de 6 dígitos recibido por correo.</summary>
    [Required(ErrorMessage = "Este campo es obligatorio")]
    public string? Codigo { get; set; }
}

/// <summary>Cuerpo de <c>POST /api/account/resend</c> (reenvío de código del registro).</summary>
public class ResendVm
{
    /// <summary>Correo al que se reenvía el código.</summary>
    [Required(ErrorMessage = "Este campo es obligatorio")]
    [EmailAddress(ErrorMessage = "Ingresa un correo válido")]
    public string? Correo { get; set; }
}

/// <summary>PB-02 HU1-PB2: cuerpo de <c>POST /api/account/login</c>.</summary>
public class LoginVm
{
    /// <summary>Correo del usuario.</summary>
    [Required(ErrorMessage = "Este campo es obligatorio")]
    [EmailAddress(ErrorMessage = "Ingresa un correo válido")]
    public string? Correo { get; set; }

    /// <summary>Contraseña del usuario.</summary>
    [Required(ErrorMessage = "Este campo es obligatorio")]
    public string? Password { get; set; }
}

/// <summary>Datos de la sesión creada en <c>POST /api/account/login</c>.</summary>
public class LoginResponseDto
{
    /// <summary>Id del usuario autenticado.</summary>
    public int IdUsuario { get; set; }

    /// <summary>Correo del usuario autenticado.</summary>
    public string Correo { get; set; } = string.Empty;

    /// <summary>Rol: `Paciente`, `Psicologo` o `Administrador`.</summary>
    public string Rol { get; set; } = string.Empty;

    /// <summary>Estado de la cuenta: `Activa`, `PendienteAprobacion`, `Suspendida`, etc.</summary>
    public string EstadoCuenta { get; set; } = string.Empty;

    /// <summary>Token de sesión opaco (se envía como `Authorization: Bearer &lt;token&gt;`).</summary>
    public string Token { get; set; } = string.Empty;

    /// <summary>Momento de expiración de la sesión (8 horas).</summary>
    public DateTime ExpiraEn { get; set; }

    /// <summary>Panel al que debe redirigir el front según el rol.</summary>
    public string Panel { get; set; } = string.Empty;
}

/// <summary>PB-05 HU1-PB5: cuerpo de <c>POST /api/account/forgot</c> y <c>/forgot/resend</c>.</summary>
public class ForgotVm
{
    /// <summary>Correo del usuario que solicita recuperar su contraseña.</summary>
    [Required(ErrorMessage = "Este campo es obligatorio")]
    [EmailAddress(ErrorMessage = "Ingresa un correo válido")]
    public string? Correo { get; set; }
}

/// <summary>PB-05 HU2-PB5: cuerpo de <c>POST /api/account/forgot/verify</c>.</summary>
public class VerifyCodeVm
{
    /// <summary>Correo del usuario que recupera su contraseña.</summary>
    [Required(ErrorMessage = "Este campo es obligatorio")]
    [EmailAddress(ErrorMessage = "Ingresa un correo válido")]
    public string? Correo { get; set; }

    /// <summary>Código de recuperación de 6 dígitos recibido por correo.</summary>
    [Required(ErrorMessage = "Este campo es obligatorio")]
    public string? Codigo { get; set; }
}

/// <summary>PB-05 HU2-PB5: cuerpo de <c>POST /api/account/forgot/reset</c>.</summary>
public class ResetPasswordVm
{
    /// <summary>Correo del usuario que restablece su contraseña.</summary>
    [Required(ErrorMessage = "Este campo es obligatorio")]
    [EmailAddress(ErrorMessage = "Ingresa un correo válido")]
    public string? Correo { get; set; }

    /// <summary>Código de recuperación ya verificado.</summary>
    [Required(ErrorMessage = "Este campo es obligatorio")]
    public string? Codigo { get; set; }

    /// <summary>Nueva contraseña: mínimo 6 caracteres, al menos una letra y un número.</summary>
    [Required(ErrorMessage = "Este campo es obligatorio")]
    public string? Password { get; set; }
}