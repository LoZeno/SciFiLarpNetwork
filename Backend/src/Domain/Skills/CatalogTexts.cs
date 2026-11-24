using SciFiLarpNetwork.Backend.Domain.Texts;

namespace SciFiLarpNetwork.Backend.Domain.Skills;

/// <summary>Rules for the optional description of a talent, skill or network. Blank means “not set”.</summary>
public static class CatalogTexts
{
    public const int MaximumDescriptionLength = 2000;

    /// <summary>A description is up to 2,000 characters after trimming, and storable (see <see cref="FreeText"/>).</summary>
    public static bool IsValidDescription(string? description) =>
        string.IsNullOrWhiteSpace(description) ||
        (description.Trim().Length <= MaximumDescriptionLength && FreeText.IsStorable(description));
}
