namespace Mindzyxs_API.Dtos;

/// <summary>Cálculo del riesgo emocional: <c>POST /api/riesgo/invitado/calcular</c> (PB-08 HU1-PB8).</summary>
public class CalcularRiesgoVm
{
    /// <summary>Id del cuestionario finalizado al 100%.</summary>
    public int CuestionarioId { get; set; }

    /// <summary>Respuestas completas de la sesión del invitado (las mismas del finalizar PB-07).</summary>
    public List<RespuestaCuestionarioVm> Respuestas { get; set; } = [];
}

/// <summary>Acción del paciente sobre la alerta de riesgo alto: <c>POST /api/riesgo/paciente/alerta</c> (PB-16 HU1-PB16).</summary>
public class RegistrarAlertaRiesgoVm
{
    /// <summary>
    /// Acción registrada en logs: "vista" (se generó y visualizó la alerta),
    /// "lineas" (Líneas de ayuda), "recomendaciones" (Ver recomendaciones) o "entendido".
    /// </summary>
    public string Accion { get; set; } = "";

    /// <summary>Origen de la alerta: "cuestionario" o "registro" (opcional, solo para el detalle del log).</summary>
    public string? Origen { get; set; }
}

/// <summary>Alerta de urgencia (solo se muestra si el riesgo es alto).</summary>
public class AlertaRiesgoDto
{
    /// <summary>True cuando el nivel es Alto.</summary>
    public bool Mostrar { get; set; }

    /// <summary>"Riesgo alto detectado. Busca ayuda profesional o comunícate con una línea de ayuda".</summary>
    public string Mensaje { get; set; } = "";
}

/// <summary>Pantalla Resultado del riesgo (Invitado): <c>POST /api/riesgo/invitado/calcular</c>.</summary>
public class RiesgoInvitadoDto
{
    /// <summary>Nivel calculado: Bajo, Medio o Alto.</summary>
    public string Nivel { get; set; } = "";

    /// <summary>Puntaje 1-5 (promedio de escala, redondeado).</summary>
    public int Puntaje { get; set; }

    /// <summary>Breve explicación general según el nivel.</summary>
    public string Explicacion { get; set; } = "";

    /// <summary>Alerta visible de urgencia (solo si Alto) + Líneas resaltadas.</summary>
    public AlertaRiesgoDto Alerta { get; set; } = new();

    /// <summary>True: mostrar botón "Líneas de ayuda" (siempre) y resaltarlo si Alto.</summary>
    public bool MostrarLineasAyuda { get; set; } = true;

    /// <summary>True: mostrar botón "Ver recomendaciones básicas".</summary>
    public bool MostrarRecomendaciones { get; set; } = true;

    /// <summary>Mensaje aclaratorio obligatorio junto al resultado (no diagnóstico).</summary>
    public bool AvisoObligatorio { get; set; } = true;
}

/// <summary>Línea de ayuda activa: <c>GET /api/ayuda/lineas</c> (PB-08 HU1-PB8).</summary>
public class LineaAyudaDto
{
    /// <summary>Id de la línea.</summary>
    public int Id { get; set; }

    /// <summary>Nombre visible.</summary>
    public string Nombre { get; set; } = "";

    /// <summary>Teléfono de contacto.</summary>
    public string? Telefono { get; set; }

    /// <summary>URL de contacto.</summary>
    public string? Url { get; set; }

    /// <summary>Horario de atención.</summary>
    public string? Horario { get; set; }

    /// <summary>Cobertura o disponibilidad.</summary>
    public string? Cobertura { get; set; }

    /// <summary>Descripción corta.</summary>
    public string? Descripcion { get; set; }

    /// <summary>WhatsApp de contacto.</summary>
    public string? Whatsapp { get; set; }

    /// <summary>Correo de contacto.</summary>
    public string? Email { get; set; }

    /// <summary>Chat en línea.</summary>
    public string? ChatUrl { get; set; }

    /// <summary>Videollamada (agendamiento).</summary>
    public string? Videollamada { get; set; }

    /// <summary>Tipo de atención (apoyo_emocional, intervencion_crisis, emergencia, violencia_genero, infancia_adolescencia, orientacion_institucional).</summary>
    public string? TipoAtencion { get; set; }

    /// <summary>Departamento (null = nacional).</summary>
    public string? Departamento { get; set; }

    /// <summary>Municipio (null = nacional/departamental).</summary>
    public string? Municipio { get; set; }

    /// <summary>Fuente oficial del contacto.</summary>
    public string? Fuente { get; set; }
}
