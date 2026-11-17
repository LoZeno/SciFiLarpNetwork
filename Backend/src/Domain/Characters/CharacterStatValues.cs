namespace SciFiLarpNetwork.Backend.Domain.Characters;

/// <summary>
/// Checks a stat value against its stat type: a whole number of 0 or more for a Number stat, and an optional
/// text of 1–64 characters for a Text stat.
/// </summary>
public static class CharacterStatValues
{
    public static bool IsValid(CharacterStatType type, int? number, string? text) =>
        type switch
        {
            CharacterStatType.Number => number is >= 0 && text is null,
            CharacterStatType.Text => number is null && CharacterTexts.IsValidOptional(text),
            _ => false
        };

    /// <summary>Returns the stored form of a valid value: Text values are trimmed, and blank text is not set.</summary>
    public static CharacterStatValue Normalize(Guid statId, int? number, string? text) =>
        new(statId, number, CharacterTexts.Normalize(text));
}
