using System.ComponentModel.DataAnnotations;

namespace Mindzyxs_API.Dtos;

// PB-01: registro (HU1 tipo cuenta + HU2 datos + HU3 tarjeta). HU4 usa VerifyVm.
public class RegisterVm
{
    [Required(ErrorMessage = "Debes seleccionar un tipo de cuenta: Paciente o Psicólogo")]
    public string? TipoUsuario { get; set; }

    [Required(ErrorMessage = "Este campo es obligatorio")]
    public string? Nombre { get; set; }

    [Required(ErrorMessage = "Este campo es obligatorio")]
    public string? Apellido { get; set; }

    [Required(ErrorMessage = "Ingresa una edad válida")]
    [Range(1, 120, ErrorMessage = "Ingresa una edad válida")]
    public int? Edad { get; set; }

    [Required(ErrorMessage = "Este campo es obligatorio")]
    public int? IdGenero { get; set; }

    [Required(ErrorMessage = "Este campo es obligatorio")]
    [EmailAddress(ErrorMessage = "Ingresa un correo válido")]
    public string? Correo { get; set; }

    [Required(ErrorMessage = "Este campo es obligatorio")]
    public string? Password { get; set; }

    // Solo psicólogo (HU3-PB1). Paciente lo envía null.
    public IFormFile? TarjetaProfesional { get; set; }
}

public class VerifyVm
{
    [Required(ErrorMessage = "Este campo es obligatorio")]
    [EmailAddress(ErrorMessage = "Ingresa un correo válido")]
    public string? Correo { get; set; }

    [Required(ErrorMessage = "Este campo es obligatorio")]
    public string? Codigo { get; set; }
}

public class ResendVm
{
    [Required(ErrorMessage = "Este campo es obligatorio")]
    [EmailAddress(ErrorMessage = "Ingresa un correo válido")]
    public string? Correo { get; set; }
}
