namespace SciFiLarpNetwork.Backend.Domain.Networks;

/// <summary>
/// Network codes: 1–32 lower-case letters <c>a</c>–<c>z</c>, digits, and hyphens, not starting or ending with a hyphen.
/// Input is trimmed and lower-cased before it is checked.
/// </summary>
public static class NetworkCodes
{
    public const int MaximumLength = 32;

    public static bool IsValid(string? code)
    {
        if (string.IsNullOrWhiteSpace(code))
        {
            return false;
        }

        var normalized = Normalize(code);
        return normalized.Length <= MaximumLength &&
               normalized[0] != '-' &&
               normalized[^1] != '-' &&
               normalized.All(character => character is >= 'a' and <= 'z' or >= '0' and <= '9' or '-');
    }

    public static string Normalize(string code) => code.Trim().ToLowerInvariant();
}
