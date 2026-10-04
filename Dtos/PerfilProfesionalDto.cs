namespace Mindzyxs_API.Dtos;

/// <summary>Perfil profesional del psicólogo (vista): <c>GET /api/psicologo/perfil-profesional</c> (PB-24 HU1-PB24).</summary>
public class PerfilProfesionalVm
{
    /// <summary>Id del psicólogo.</summary>
    public int Id { get; set; }

    /// <summary>Nombre completo.</summary>
    public string Nombre { get; set; } = "";

    /// <summary>Correo electrónico.</summary>
    public string Correo { get; set; } = "";

    /// <summary>Años de experiencia.</summary>
    public int? AñosExperiencia { get; set; }

    /// <summary>Descripción profesional.</summary>
    public string? DescripcionProfesional { get; set; }

    /// <summary>Datos de contacto (teléfono, dirección, etc.).</summary>
    public string? DatosContacto { get; set; }

    /// <summary>Tarjeta profesional verificada.</summary>
    public bool TarjetaProfesionalVerificada { get; set; }

    /// <summary>Especialidades (nombres).</summary>
    public List<string> Especialidades { get; set; } = [];

    /// <summary>Indica si la tarjeta profesional tiene archivo subido (FilePath).</summary>
    public bool TieneTarjetaArchivo { get; set; }
}

/// <summary>Cuerpo de <c>PUT /api/psicologo/perfil-profesional</c> (PB-24 HU1-PB24).</summary>
public class ActualizarPerfilProfesionalVm
{
    /// <summary>Años de experiencia (opcional, >= 0).</summary>
    public int? AñosExperiencia { get; set; }

    /// <summary>Descripción profesional (opcional).</summary>
    public string? DescripcionProfesional { get; set; }

    /// <summary>Datos de contacto (teléfono, dirección, etc.) (opcional).</summary>
    public string? DatosContacto { get; set; }

    /// <summary>Tarjeta profesional verificada (opcional, solo admin puede poner true).</summary>
    public bool? TarjetaProfesionalVerificada { get; set; }

    /// <summary>Ids de especialidades (vacío = ninguna).</summary>
    public List<int> Especialidades { get; set; } = [];

    /// <summary>Correo de contacto (opcional, se valida unicidad).</summary>
    public string? Correo { get; set; }
}