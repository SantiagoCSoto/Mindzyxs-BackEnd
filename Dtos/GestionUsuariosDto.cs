namespace Mindzyxs_API.Dtos;

/// <summary>Usuario en gestión: <c>GET /api/admin/usuarios</c> (PB-25 HU1-PB25).</summary>
public class UsuarioAdminDto
{
    /// <summary>Id del usuario.</summary>
    public int Id { get; set; }

    /// <summary>Nombre completo.</summary>
    public string Nombre { get; set; } = string.Empty;

    /// <summary>Correo.</summary>
    public string Correo { get; set; } = string.Empty;

    /// <summary>Rol (Paciente/Psicologo).</summary>
    public string Rol { get; set; } = string.Empty;

    /// <summary>Estado de cuenta.</summary>
    public string Estado { get; set; } = string.Empty;

    /// <summary>Estado de aprobación (psicólogos).</summary>
    public string? Aprobacion { get; set; }

    /// <summary>Fecha de registro.</summary>
    public DateTime FechaRegistro { get; set; }
}

/// <summary>Detalle de usuario: <c>GET /api/admin/usuarios/{id}</c> (PB-25).</summary>
public class UsuarioDetalleDto : UsuarioAdminDto
{
    /// <summary>Nombres.</summary>
    public string Nombres { get; set; } = string.Empty;

    /// <summary>Apellidos.</summary>
    public string Apellidos { get; set; } = string.Empty;

    /// <summary>Fecha de nacimiento.</summary>
    public DateTime? FechaNacimiento { get; set; }

    /// <summary>Género.</summary>
    public string? Genero { get; set; }
}

/// <summary>Editar usuario: <c>PUT /api/admin/usuarios/{id}</c> (PB-25).</summary>
public class ActualizarUsuarioVm
{
    /// <summary>Nombres (obligatorio).</summary>
    public string Nombres { get; set; } = string.Empty;

    /// <summary>Apellidos.</summary>
    public string? Apellidos { get; set; }

    /// <summary>Correo (obligatorio, único).</summary>
    public string Correo { get; set; } = string.Empty;

    /// <summary>Fecha de nacimiento.</summary>
    public DateTime? FechaNacimiento { get; set; }

    /// <summary>Id de género.</summary>
    public int? IdGenero { get; set; }
}

/// <summary>Cambiar estado: <c>PUT /api/admin/usuarios/{id}/estado</c> (PB-25).</summary>
public class CambiarEstadoVm
{
    /// <summary>Nombre del estado (Activa, Suspendida, Inactiva...).</summary>
    public string NuevoEstado { get; set; } = string.Empty;
}
