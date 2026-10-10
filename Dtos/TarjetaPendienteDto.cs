namespace Mindzyxs_API.Dtos;

/// <summary>Psicólogo en cola de verificación: <c>GET /api/admin/psicologos/tarjeta-pendientes</c> (PB-25).</summary>
public class PsicologoTarjetaPendienteDto
{
    /// <summary>Id del psicólogo.</summary>
    public int Id { get; set; }

    /// <summary>Nombre completo.</summary>
    public string Nombre { get; set; } = string.Empty;

    /// <summary>Correo.</summary>
    public string Correo { get; set; } = string.Empty;

    /// <summary>False si subió tarjeta nueva pendiente de revisión.</summary>
    public bool TarjetaVerificada { get; set; }

    /// <summary>Especialidades ocultas hasta verificación (nombres).</summary>
    public List<string> EspecialidadesPendientes { get; set; } = [];
}
