namespace Mindzyxs_API.Dtos;

// PB-27 HU1-PB27: Administrar BD (respaldos locales + mantenimiento).
public class BackupResultDto
{
    public int IdRespaldo { get; set; }
    public string NombreArchivo { get; set; } = string.Empty;
    public string Tamano { get; set; } = string.Empty;
    public DateTime FechaHora { get; set; }
    public string Ubicacion { get; set; } = string.Empty;
    public string Tipo { get; set; } = "bak";
}

public class BackupListItemDto
{
    public int IdRespaldo { get; set; }
    public string NombreArchivo { get; set; } = string.Empty;
    public string? Tamano { get; set; }
    public DateTime FechaHora { get; set; }
}

public class MaintenanceResultDto
{
    public int CodigosExpiradosEliminados { get; set; }
    public int SesionesExpiradasEliminadas { get; set; }
    public int LogsAntiguosEliminados { get; set; }
}