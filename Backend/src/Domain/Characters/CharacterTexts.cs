using SciFiLarpNetwork.Backend.Domain.Skills;

namespace SciFiLarpNetwork.Backend.Domain.Characters;

/// <summary>
/// Rules for a character's optional free-text fields (title, species, gender, type) and Text stat values:
/// 1–64 characters after trimming when present. Blank values mean “not set”.
/// </summary>
public static class CharacterTexts
{
    public static bool IsValidOptional(string? text) => Normalize(text) is null || CatalogNames.IsValid(text);

    public static string? Normalize(string? text) => string.IsNullOrWhiteSpace(text) ? null : text.Trim();
}
