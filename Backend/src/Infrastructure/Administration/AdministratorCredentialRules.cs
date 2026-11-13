using System.Text;
using System.Text.RegularExpressions;

namespace SciFiLarpNetwork.Backend.Infrastructure.Administration;

internal static class AdministratorCredentialRules
{
    private static readonly Regex UserNamePattern = new("^[\\p{L}\\p{N}][\\p{L}\\p{N} ._@+-]{2,63}$", RegexOptions.Compiled | RegexOptions.CultureInvariant);

    public static bool AreValid(string userName, string password)
    {
        if (string.IsNullOrWhiteSpace(userName) || string.IsNullOrWhiteSpace(password))
        {
            return false;
        }

        var trimmed = userName.Trim();
        if (trimmed.Length is < 3 or > 64 || !UserNamePattern.IsMatch(trimmed))
        {
            return false;
        }

        return password.Length is >= 12 and <= 128;
    }

    public static string NormalizeUserName(string userName)
    {
        var trimmed = userName.Trim();
        return Encoding.UTF8.GetString(
            Encoding.UTF8.GetBytes(trimmed.Normalize(NormalizationForm.FormKC))).ToUpperInvariant();
    }
}
