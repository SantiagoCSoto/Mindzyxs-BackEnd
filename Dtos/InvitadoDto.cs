namespace Mindzyxs_API.Dtos;

/// <summary>Registro de acceso en modo invitado: <c>POST /api/invitado/acceso</c> (PB-06 HU1-PB6).</summary>
public class AccesoInvitadoVm
{
    /// <summary>Origen del acceso: <c>landing</c> (botón Modo invitado) o <c>header</c>.</summary>
    public string? Origen { get; set; }

    /// <summary>Resultado del acceso: <c>ok</c> o <c>error</c>.</summary>
    public string? Resultado { get; set; }
}

/// <summary>Registro de intento bloqueado de una función exclusiva: <c>POST /api/invitado/bloqueo</c>.</summary>
public class BloqueoInvitadoVm
{
    /// <summary>Nombre de la función exclusiva que el invitado intentó usar (ej. <c>Historial emocional</c>).</summary>
    public string? Funcion { get; set; }
}

/// <summary>Registro de salida del modo invitado: <c>POST /api/invitado/salida</c>.</summary>
public class SalidaInvitadoVm
{
    /// <summary>Origen de la salida: <c>panel</c> (botón Volver a la Landing).</summary>
    public string? Origen { get; set; }
}

/// <summary>Panel del invitado: opciones disponibles y funciones bloqueadas (PB-06 HU1-PB6).</summary>
public class InvitadoPanelDto
{
    /// <summary>Indica si el mensaje aclaratorio (no diagnóstico) es obligatorio antes de las funciones sensibles.</summary>
    public bool AvisoObligatorio { get; set; }

    /// <summary>Opciones básicas disponibles para el invitado (Responder cuestionario, Recomendaciones, Líneas de ayuda).</summary>
    public List<InvitadoOpcionDto> Disponibles { get; set; } = [];

    /// <summary>Funciones exclusivas de usuarios registrados (bloqueadas para el invitado).</summary>
    public List<InvitadoOpcionDto> Exclusivas { get; set; } = [];
}

/// <summary>Opción del panel de invitado: identificador, título y descripción corta.</summary>
public class InvitadoOpcionDto
{
    /// <summary>Código corto de la opción (query de navegación o permiso).</summary>
    public string Codigo { get; set; } = "";

    /// <summary>Título visible de la opción.</summary>
    public string Titulo { get; set; } = "";

    /// <summary>Descripción corta mostrada bajo el título.</summary>
    public string Descripcion { get; set; } = "";
}