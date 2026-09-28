namespace Mindzyxs_API.Dtos;

/// <summary>Editar perfil del paciente: <c>PUT /api/account/perfil</c> (PB-10 HU1-PB10).</summary>
public class ActualizarPerfilVm
{
    /// <summary>Nombres (obligatorio).</summary>
    public string? Nombres { get; set; }

    /// <summary>Apellidos (obligatorios).</summary>
    public string? Apellidos { get; set; }

    /// <summary>Fecha de nacimiento (no futura, mayor de 14 años).</summary>
    public DateTime? FechaNacimiento { get; set; }

    /// <summary>Id del género (debe existir).</summary>
    public int? IdGenero { get; set; }
}

/// <summary>Solicitar cambio de correo: <c>POST /api/account/correo/solicitar</c>.</summary>
public class SolicitarCambioCorreoVm
{
    /// <summary>Nuevo correo (formato válido y no registrado).</summary>
    public string? NuevoCorreo { get; set; }
}

/// <summary>Confirmar cambio de correo: <c>POST /api/account/correo/confirmar</c>.</summary>
public class ConfirmarCambioCorreoVm
{
    /// <summary>Nuevo correo al que se envió el código.</summary>
    public string? NuevoCorreo { get; set; }

    /// <summary>Código de 6 dígitos.</summary>
    public string? Codigo { get; set; }
}

/// <summary>Reanudar verificación: <c>POST /api/account/verificacion/reanudar</c>.</summary>
public class ReanudarVerificacionVm
{
    /// <summary>Correo del registro incompleto.</summary>
    public string? Correo { get; set; }
}
