using System.Security.Cryptography;

namespace Mindzyxs_API.Utilities;

public static class CodeHelper
{
    public static string NewCode6()
    {
        var n = RandomNumberGenerator.GetInt32(100_000, 1_000_000);
        return n.ToString();
    }
}

public static class DateTimeProvider
{
    public static DateTime UtcNow() => DateTime.UtcNow;
}

public static class AgeHelper
{
    // Acuerdo PB-01: el API recibe Edad y calcula fecha_nacimiento.
    public static DateTime ToBirthDate(int edad)
        => DateTime.Today.AddYears(-edad);
}
