using System.Net.Mail;

namespace Mindzyxs_API.Utilities;

public static class ContactHelper
{
    public static string Normalize(string? value) => (value ?? string.Empty).Trim();

    public static bool IsEmail(string? value)
    {
        if (string.IsNullOrWhiteSpace(value)) return false;
        try
        {
            var addr = new MailAddress(value.Trim());
            return addr.Address == value.Trim();
        }
        catch
        {
            return false;
        }
    }

    public static bool IsValidTipoUsuario(string? value)
        => string.Equals(Normalize(value), AppRoles.Paciente, StringComparison.OrdinalIgnoreCase)
        || string.Equals(Normalize(value), AppRoles.Psicologo, StringComparison.OrdinalIgnoreCase);

    public static string CanonicalTipoUsuario(string? value)
    {
        var v = Normalize(value);
        return string.Equals(v, AppRoles.Psicologo, StringComparison.OrdinalIgnoreCase)
            ? AppRoles.Psicologo
            : AppRoles.Paciente;
    }
}
