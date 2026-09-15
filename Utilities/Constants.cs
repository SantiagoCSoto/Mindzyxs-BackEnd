namespace Mindzyxs_API.Utilities;

public static class AppRoles
{
    public const string Paciente = "Paciente";
    public const string Psicologo = "Psicologo";
}

// Nombres exactos de dbo.estados (02_seed_catalogos.sql).
public static class AccountStates
{
    public const string Activa = "Activa";
    public const string PendienteVerificacion = "PendienteVerificacion";
    public const string PendienteAprobacion = "PendienteAprobacion";
    public const string Aprobado = "Aprobado";
}

public static class VerificationTypes
{
    public const string Registro = "Registro";
}
