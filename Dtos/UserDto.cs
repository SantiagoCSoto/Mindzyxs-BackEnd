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

    /// <summary>Fecha de nacimiento calculada desde la edad al registrarse.</summary>
    public DateTime? FechaNacimiento { get; set; }

    /// <summary>Fecha de registro.</summary>
    public DateTime FechaRegistro { get; set; }

    /// <summary>Id del género.</summary>
    public int? IdGenero { get; set; }

    /// <summary>Ruta física de la tarjeta profesional (solo psicólogos) en disco.</summary>
    public string? FilePath { get; set; }
}