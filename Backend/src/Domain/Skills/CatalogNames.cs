using System.Text;

namespace SciFiLarpNetwork.Backend.Domain.Skills;

/// <summary>
/// Naming rules shared by talents, skills, and card flags.
/// </summary>
public static class CatalogNames
{
    public const int MaximumLength = 64;

    public static bool IsValid(string? name)
    {
        if (string.IsNullOrWhiteSpace(name))
        {
            return false;
        }

        var trimmed = name.Trim();
        return trimmed.Length <= MaximumLength && !trimmed.Any(char.IsControl);
    }

    /// <summary>
    /// Returns the form used for uniqueness: trimmed, Unicode compatibility-normalized, and upper-cased.
    /// </summary>
    public static string Normalize(string name) =>
        name.Trim().Normalize(NormalizationForm.FormKC).ToUpperInvariant();
}
