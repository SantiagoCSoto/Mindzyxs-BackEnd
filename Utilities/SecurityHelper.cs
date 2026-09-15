using System.Security.Cryptography;

namespace Mindzyxs_API.Utilities;

// PBKDF2-SHA256 propio (formato: v1$iter$salt$hash). No depende de paquetes externos.
public static class PasswordHelper
{
    private const int Iterations = 210_000;
    private const int SaltLen = 16;
    private const int KeyLen = 32;

    public static bool IsStrongEnough(string? password)
    {
        if (string.IsNullOrEmpty(password) || password.Length < 6) return false;
        return password.Any(char.IsLetter) && password.Any(char.IsDigit);
    }

    public static string Hash(string password)
    {
        var salt = RandomNumberGenerator.GetBytes(SaltLen);
        var key = Rfc2898DeriveBytes.Pbkdf2(password, salt, Iterations, HashAlgorithmName.SHA256, KeyLen);
        return $"v1${Iterations}${Convert.ToBase64String(salt)}${Convert.ToBase64String(key)}";
    }

    public static bool Verify(string password, string? stored)
    {
        if (string.IsNullOrWhiteSpace(stored)) return false;
        var parts = stored.Split('$');
        if (parts.Length != 4 || parts[0] != "v1") return false;
        if (!int.TryParse(parts[1], out var iters)) return false;
        var salt = Convert.FromBase64String(parts[2]);
        var expected = Convert.FromBase64String(parts[3]);
        var actual = Rfc2898DeriveBytes.Pbkdf2(password ?? string.Empty, salt, iters, HashAlgorithmName.SHA256, expected.Length);
        return CryptographicOperations.FixedTimeEquals(actual, expected);
    }
}
