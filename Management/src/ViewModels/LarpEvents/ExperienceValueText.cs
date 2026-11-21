using System.Globalization;

namespace SciFiLarpNetwork.Management.ViewModels.LarpEvents;

/// <summary>Reads a typed base or day experience value: blank for none, or a whole number of 0 or more.</summary>
internal static class ExperienceValueText
{
    public static bool TryParse(string text, out int? value)
    {
        value = null;
        if (string.IsNullOrWhiteSpace(text))
        {
            return true;
        }

        if (!int.TryParse(text.Trim(), NumberStyles.None, CultureInfo.InvariantCulture, out var parsed))
        {
            return false;
        }

        value = parsed;
        return true;
    }

    /// <summary>Reads a value already checked with <see cref="TryParse"/>.</summary>
    public static int? Parse(string text) =>
        TryParse(text, out var value) ? value : throw new InvalidOperationException("The experience value was not checked.");

    public static string Format(int? value) => value?.ToString(CultureInfo.InvariantCulture) ?? string.Empty;
}
