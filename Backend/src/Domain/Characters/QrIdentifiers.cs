namespace SciFiLarpNetwork.Backend.Domain.Characters;

/// <summary>
/// Public character QR identifiers: random 128-bit values written as 32 lower-case hexadecimal digits. They are
/// printed on cards and are never authentication credentials.
/// </summary>
public static class QrIdentifiers
{
    public static string Create() => Guid.NewGuid().ToString("N");

    /// <summary>Accepts any standard GUID text form, and returns the stored 32-digit form.</summary>
    public static bool TryNormalize(string? input, out string identifier)
    {
        if (Guid.TryParse(input?.Trim(), out var value))
        {
            identifier = value.ToString("N");
            return true;
        }

        identifier = string.Empty;
        return false;
    }
}
