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

// PB-07: dbo.cuestionarios (Predeterminado/Publicado para invitados y pacientes;
// Personalizado + asignaciones_cuestionario para pacientes específicos).
public class CuestionarioRow
{
    public int IdCuestionario { get; set; }
    public string Titulo { get; set; } = string.Empty;
    public string? Descripcion { get; set; }
    public string Tipo { get; set; } = string.Empty;
    public string Estado { get; set; } = string.Empty;
    public int IdCreador { get; set; }
    public DateTime FechaCreacion { get; set; }
}

// PB-07: dbo.preguntas. Todas son siempre obligatorias (sin flag).
// tipo: 'escala' (entra al promedio de puntaje) u otros ('binaria', 'opcion_multiple').
public class PreguntaRow
{
    public int IdPregunta { get; set; }
    public int IdCuestionario { get; set; }
    public string Texto { get; set; } = string.Empty;
    public int Orden { get; set; }
    public string? Tipo { get; set; }
    public int? PuntajeMax { get; set; }
}

// PB-07: dbo.opciones_pregunta (texto, valor numérico para el puntaje, orden).
public class OpcionPreguntaRow
{
    public int IdOpcion { get; set; }
    public int IdPregunta { get; set; }
    public string Texto { get; set; } = string.Empty;
    public int Valor { get; set; }
    public int Orden { get; set; }
}

// PB-08: dbo.lineas_ayuda (solo se sirven las Activas a invitados y pacientes).
public class LineaAyudaRow
{
    public int IdLinea { get; set; }
    public string Nombre { get; set; } = string.Empty;
    public string? Telefono { get; set; }
    public string? Url { get; set; }
    public string? Horario { get; set; }
    public string? Cobertura { get; set; }
    public string Estado { get; set; } = string.Empty;
    public string? Descripcion { get; set; }
}
