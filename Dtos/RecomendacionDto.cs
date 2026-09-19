namespace Mindzyxs_API.Dtos;

/// <summary>Ítem de la lista de recomendaciones básicas: <c>GET /api/recomendaciones/invitado</c> (PB-09 HU1-PB9).</summary>
public class RecomendacionItemDto
{
    /// <summary>Id de la recomendación.</summary>
    public int Id { get; set; }

    /// <summary>Título visible.</summary>
    public string Titulo { get; set; } = "";

    /// <summary>Intro corta.</summary>
    public string? Intro { get; set; }

    /// <summary>Icono (emoji).</summary>
    public string? Icono { get; set; }

    /// <summary>Nivel al que aplica (Bajo/Medio/Alto; null = transversal).</summary>
    public string? Nivel { get; set; }

    /// <summary>True si es la prioritaria para el nivel de la sesión.</summary>
    public bool Prioritaria { get; set; }
}

/// <summary>Lista de recomendaciones básicas del invitado (priorizada por nivel).</summary>
public class RecomendacionesInvitadoDto
{
    /// <summary>False cuando el admin no ha cargado contenido.</summary>
    public bool Disponible { get; set; }

    /// <summary>"No hay recomendaciones disponibles en este momento" si no hay contenido.</summary>
    public string? Mensaje { get; set; }

    /// <summary>Nivel de riesgo de la sesión usado para priorizar (si venía).</summary>
    public string? Nivel { get; set; }

    /// <summary>Recomendaciones ordenadas (nivel de sesión primero).</summary>
    public List<RecomendacionItemDto> Recomendaciones { get; set; } = [];
}

/// <summary>Detalle de una recomendación: <c>GET /api/recomendaciones/invitado/{id}</c>.</summary>
public class RecomendacionDetalleDto
{
    /// <summary>Id de la recomendación.</summary>
    public int Id { get; set; }

    /// <summary>Título visible.</summary>
    public string Titulo { get; set; } = "";

    /// <summary>Intro corta.</summary>
    public string? Intro { get; set; }

    /// <summary>Icono (emoji).</summary>
    public string? Icono { get; set; }

    /// <summary>Nivel al que aplica (null = transversal).</summary>
    public string? Nivel { get; set; }

    /// <summary>Duración sugerida (ej. "Duración sugerida: 5 minutos").</summary>
    public string? Duracion { get; set; }

    /// <summary>Pasos a seguir.</summary>
    public List<string> Pasos { get; set; } = [];

    /// <summary>Instrucciones y advertencias (autocuidado, no clínico).</summary>
    public string? Instrucciones { get; set; }

    /// <summary>Mensaje aclaratorio (no diagnóstico) junto al contenido.</summary>
    public bool AvisoObligatorio { get; set; } = true;
}
