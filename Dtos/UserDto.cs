namespace Mindzyxs_API.Dtos;

/// <summary>
/// Refleja la fila de `dbo.usuarios` de Mindzyxs-BD: `file_path`, sin blob, sin celular/dirección.
/// </summary>
public class UsuarioDto
{
    /// <summary>Id del usuario.</summary>
    public int IdUsuario { get; set; }

    /// <summary>Id del rol (`Paciente`, `Psicologo`, `Administrador`).</summary>
    public int IdRol { get; set; }

    /// <summary>Id del estado de la cuenta (`PendienteVerificacion`, `Activa`, `PendienteAprobacion`, `Suspendida`).</summary>
    public int IdEstadoCuenta { get; set; }

    /// <summary>Id del estado de aprobación (para psicólogos).</summary>
    public int? IdEstadoAprobacion { get; set; }

    /// <summary>Nombre(s) del usuario.</summary>
    public string Nombre { get; set; } = string.Empty;

    /// <summary>Apellido(s) del usuario.</summary>
    public string Apellido { get; set; } = string.Empty;

    /// <summary>Correo electrónico (único).</summary>
    public string Correo { get; set; } = string.Empty;

    /// <summary>Hash de contraseña (PBKDF2), nunca se expone.</summary>
    public string PasswordHash { get; set; } = string.Empty;

    /// <summary>Fecha de nacimiento del usuario (no puede ser futura ni menor a 14 años).</summary>
    public DateTime? FechaNacimiento { get; set; }

    /// <summary>Fecha de registro.</summary>
    public DateTime FechaRegistro { get; set; }

    /// <summary>Id del género.</summary>
    public int? IdGenero { get; set; }

    /// <summary>Ruta física de la tarjeta profesional (solo psicólogos) en disco.</summary>
    public string? FilePath { get; set; }

    // PB-24 HU1-PB24: perfil profesional del psicólogo
    /// <summary>Años de experiencia del psicólogo.</summary>
    public int? AñosExperiencia { get; set; }

    /// <summary>Descripción profesional del psicólogo.</summary>
    public string? DescripcionProfesional { get; set; }

    /// <summary>Datos de contacto adicionales (teléfono, dirección, etc.).</summary>
    public string? DatosContacto { get; set; }

    /// <summary>Indica si la tarjeta profesional ha sido verificada por el administrador.</summary>
    public bool TarjetaProfesionalVerificada { get; set; }

    /// <summary>Especialidades del psicólogo (nombres).</summary>
    public List<string> Especialidades { get; set; } = [];
}