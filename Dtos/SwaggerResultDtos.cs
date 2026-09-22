namespace Mindzyxs_API.Dtos;

/// <summary>Cuerpo de error estándar: `{ "error": "mensaje" }`.</summary>
public class ErrorResult
{
    /// <summary>Mensaje de error exacto (según HU).</summary>
    public string Error { get; set; } = "";
}

/// <summary>Cuerpo de respuesta simple con mensaje: `{ "mensaje": "..." }`.</summary>
public class MessageResult
{
    /// <summary>Mensaje descriptivo de la operación.</summary>
    public string Mensaje { get; set; } = "";
}

/// <summary>Respuesta de <c>POST /api/account/register</c>.</summary>
public class RegisterResult
{
    /// <summary>Id del usuario recién creado.</summary>
    public int IdUsuario { get; set; }

    /// <summary>Mensaje de confirmación del registro.</summary>
    public string Mensaje { get; set; } = "";
}

/// <summary>Respuesta de <c>POST /api/account/verify</c>.</summary>
public class VerifyResult
{
    /// <summary>Estado de la cuenta tras la verificación: <c>Activa</c> o <c>PendienteAprobacion</c>.</summary>
    public string Estado { get; set; } = "";

    /// <summary>Mensaje de confirmación de la verificación.</summary>
    public string Mensaje { get; set; } = "";
}

/// <summary>Respuesta de <c>POST /api/account/login</c>.</summary>
public class LoginResult
{
    /// <summary>Mensaje de confirmación del inicio de sesión.</summary>
    public string Mensaje { get; set; } = "";

    /// <summary>Token de sesión opaco (se envía como <c>Authorization: Bearer &lt;token&gt;</c>).</summary>
    public string Token { get; set; } = "";

    /// <summary>Momento de expiración de la sesión (8 horas).</summary>
    public DateTime ExpiraEn { get; set; }

    /// <summary>Datos básicos del usuario autenticado.</summary>
    public LoginUsuarioResult Usuario { get; set; } = new();
}

/// <summary>Datos del usuario devueltos en <c>POST /api/account/login</c>.</summary>
public class LoginUsuarioResult
{
    /// <summary>Id del usuario.</summary>
    public int Id { get; set; }

    /// <summary>Correo electrónico del usuario.</summary>
    public string Correo { get; set; } = "";

    /// <summary>Rol: <c>Paciente</c>, <c>Psicologo</c> o <c>Administrador</c>.</summary>
    public string Rol { get; set; } = "";

    /// <summary>Panel al que debe redirigir el front según el rol.</summary>
    public string Panel { get; set; } = "";

    /// <summary>Estado de la cuenta: <c>Activa</c>, <c>PendienteAprobacion</c>, <c>Suspendida</c>, etc.</summary>
    public string Estado { get; set; } = "";
}

/// <summary>Respuesta de <c>GET /api/account/me</c>.</summary>
public class MeResult
{
    /// <summary>Id del usuario autenticado.</summary>
    public int IdUsuario { get; set; }

    /// <summary>Correo del usuario autenticado.</summary>
    public string Correo { get; set; } = "";

    /// <summary>Nombre del usuario.</summary>
    public string Nombre { get; set; } = "";

    /// <summary>Apellido del usuario.</summary>
    public string Apellido { get; set; } = "";
}