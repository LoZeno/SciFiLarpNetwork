using SciFiLarpNetwork.Backend.Domain.Texts;

namespace SciFiLarpNetwork.Backend.Domain.Items;

/// <summary>Rules for an item's description and effect, cost and charges.</summary>
public static class ItemTexts
{
    public const int MaximumTextLength = 4000;

    public const int MaximumCost = 999_999;

    public const int MaximumCharges = 999;

    /// <summary>
    /// A description or effect is up to 4,000 characters after trimming and storable (see <see cref="FreeText"/>).
    /// Blank means “not set”.
    /// </summary>
    public static bool IsValidText(string? text) =>
        string.IsNullOrWhiteSpace(text) || (text.Trim().Length <= MaximumTextLength && FreeText.IsStorable(text));

    public static string? Normalize(string? text) => string.IsNullOrWhiteSpace(text) ? null : text.Trim();

    /// <summary>A cost is a whole number from 0 (priceless) to 999,999.</summary>
    public static bool IsValidCost(int cost) => cost is >= 0 and <= MaximumCost;

    /// <summary>Charges are either not set or a whole number from 1 to 999.</summary>
    public static bool IsValidCharges(int? charges) => charges is null or >= 1 and <= MaximumCharges;
}
