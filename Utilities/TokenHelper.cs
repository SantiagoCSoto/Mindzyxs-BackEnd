using System.Security.Cryptography;
using System.Text;

namespace Mindzyxs_API.Utilities;

// PB-02: token opaco para dbo.sesiones.token_hash.
public static class TokenHelper
{
    public static string Generate()
    {
        var bytes = RandomNumberGenerator.GetBytes(32);
        // 43 chars url-safe
        return Convert.ToBase64String(bytes).TrimEnd('=').Replace('+', '-').Replace('/', '_');
    }

    public static string Hash(string token)
    {
        var hash = SHA256.HashData(Encoding.UTF8.GetBytes(token));
        return Convert.ToBase64String(hash);
    }
}
