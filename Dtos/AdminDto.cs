namespace Mindzyxs_API.Dtos;

/// <summary>PB-27 HU1-PB27: datos del respaldo creado por <c>POST /api/admin/backup</c>.</summary>
public class BackupResultDto
{
    /// <summary>Id del respaldo en `dbo.respaldos`.</summary>
    public int IdRespaldo { get; set; }

    /// <summary>Nombre del archivo de respaldo (ej. `Mindzyxs_20260916_1030.bak`).</summary>
    public string NombreArchivo { get; set; } = string.Empty;

    /// <summary>Tamaño del archivo en formato legible (ej. `960.5 KB`).</summary>
    public string Tamano { get; set; } = string.Empty;

    /// <summary>Fecha y hora en que se generó el respaldo.</summary>
    public DateTime FechaHora { get; set; }

    /// <summary>Ruta física del archivo en el disco del servidor.</summary>
    public string Ubicacion { get; set; } = string.Empty;

    /// <summary>Tipo de respaldo: `bak` (SQL nativo) o `json` (contingencia).</summary>
    public string Tipo { get; set; } = "bak";
}

/// <summary>Elemento de la lista de respaldos de <c>GET /api/admin/backups</c>.</summary>
public class BackupListItemDto
{
    /// <summary>Id del respaldo en `dbo.respaldos`.</summary>
    public int IdRespaldo { get; set; }

    /// <summary>Nombre del archivo de respaldo.</summary>
    public string NombreArchivo { get; set; } = string.Empty;

    /// <summary>Tamaño del archivo en formato legible.</summary>
    public string? Tamano { get; set; }

    /// <summary>Fecha y hora en que se generó el respaldo.</summary>
    public DateTime FechaHora { get; set; }
}

/// <summary>Detalle de registros limpiados por el mantenimiento.</summary>
public class MaintenanceResultDto
{
    /// <summary>Cantidad de códigos de verificación expirados eliminados.</summary>
    public int CodigosExpiradosEliminados { get; set; }

    /// <summary>Cantidad de sesiones expiradas eliminadas.</summary>
    public int SesionesExpiradasEliminadas { get; set; }

    /// <summary>Cantidad de logs de `dbo.auth` antiguos eliminados (retención 90 días).</summary>
    public int LogsAntiguosEliminados { get; set; }
}

/// <summary>Respuesta de <c>POST /api/admin/backup</c>.</summary>
public class BackupCreatedResult
{
    /// <summary>Mensaje de confirmación del respaldo.</summary>
    public string Mensaje { get; set; } = "";

    /// <summary>Datos del respaldo creado.</summary>
    public BackupResultDto Respaldo { get; set; } = new();
}

/// <summary>Respuesta de <c>GET /api/admin/backups</c>.</summary>
public class BackupListResult
{
    /// <summary>Lista de respaldos registrados (más recientes primero).</summary>
    public List<BackupListItemDto> Respaldos { get; set; } = [];
}

/// <summary>Respuesta de <c>POST /api/admin/maintenance</c>.</summary>
public class MaintenanceResult
{
    /// <summary>Mensaje de confirmación del mantenimiento.</summary>
    public string Mensaje { get; set; } = "";

    /// <summary>Detalle de registros limpiados.</summary>
    public MaintenanceResultDto Detalle { get; set; } = new();
}