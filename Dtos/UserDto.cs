namespace Mindzyxs_API.Dtos;

// Refleja dbo.usuarios de Mindzyxs-BD (22 tablas): file_path, sin blob, sin celular/direccion.
public class UsuarioDto
{
    public int IdUsuario { get; set; }
    public int IdRol { get; set; }
    public int IdEstadoCuenta { get; set; }
    public int? IdEstadoAprobacion { get; set; }
    public string Nombre { get; set; } = string.Empty;
    public string Apellido { get; set; } = string.Empty;
    public string Correo { get; set; } = string.Empty;
    public string PasswordHash { get; set; } = string.Empty;
    public DateTime? FechaNacimiento { get; set; }
    public DateTime FechaRegistro { get; set; }
    public int? IdGenero { get; set; }
    public string? FilePath { get; set; }
}
