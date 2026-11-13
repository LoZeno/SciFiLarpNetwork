using System.Security.Cryptography;
using System.Text;

namespace SciFiLarpNetwork.Backend.Infrastructure.Administration;

internal static class AdministratorSecrets
{
    public static string CreateRandomToken()
    {
        var bytes = new byte[32];
        RandomNumberGenerator.Fill(bytes);
        return Convert.ToHexString(bytes);
    }

    public static string HashSecret(string secret) =>
        Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(secret)));
}
