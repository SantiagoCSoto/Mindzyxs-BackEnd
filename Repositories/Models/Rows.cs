namespace Mindzyxs_API.Repositories.Models;

// Reflejan Mindzyxs-BD (01_create_tables_sqlserver.sql). Sin EDMX: POCOs EF Core.
public class RolRow
{
    public int IdRol { get; set; }
    public string Nombre { get; set; } = string.Empty;
}

public class GeneroRow
{
    public int IdGenero { get; set; }
    public string Nombre { get; set; } = string.Empty;
}

public class EstadoRow
{
    public int IdEstado { get; set; }
    public string Nombre { get; set; } = string.Empty;
}

public class UsuarioRow
{
    public int IdUsuario { get; set; }
    public string Nombre { get; set; } = string.Empty;
    public string Apellido { get; set; } = string.Empty;
    public string Correo { get; set; } = string.Empty;
    public string PasswordHash { get; set; } = string.Empty;
    public DateTime? FechaNacimiento { get; set; }
    public int IdRol { get; set; }
    public int IdEstadoCuenta { get; set; }
    public int? IdEstadoAprobacion { get; set; }
    public int? IdGenero { get; set; }
    public string? FilePath { get; set; }
    public DateTime FechaRegistro { get; set; }
    public DateTime? UltimoAcceso { get; set; }
}

public class CodigoRow
{
    public int IdCodigo { get; set; }
    public int IdUsuario { get; set; }
    public string Codigo { get; set; } = string.Empty;
    public string Tipo { get; set; } = string.Empty;
    public DateTime ExpiraEn { get; set; }
    public DateTime CreadoEn { get; set; }
}

public class SesionRow
{
    public int IdSesion { get; set; }
    public int IdUsuario { get; set; }
    public string TokenHash { get; set; } = string.Empty;
    public DateTime ExpiraEn { get; set; }
    public DateTime CreadaEn { get; set; }
}

public class AuthRow
{
    public int IdAuth { get; set; }
    public int? ActorIdUsuario { get; set; }
    public int? TargetIdUsuario { get; set; }
    public string Accion { get; set; } = string.Empty;
    public string? Detalle { get; set; }
    public DateTime Fecha { get; set; }
    public string? IpAddress { get; set; }
}

public class RespaldoRow
{
    public int IdRespaldo { get; set; }
    public string NombreArchivo { get; set; } = string.Empty;
    public string? Tamano { get; set; }
    public DateTime FechaHora { get; set; }
    public int IdAdmin { get; set; }
}
