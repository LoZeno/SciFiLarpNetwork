namespace SciFiLarpNetwork.Backend.Domain.LarpEvents;

/// <summary>Rules for an event's optional description. Blank means “not set”.</summary>
public static class LarpEventTexts
{
    public const int MaximumDescriptionLength = 2000;

    public static bool IsValidDescription(string? description) =>
        Normalize(description) is not { } trimmed || trimmed.Length <= MaximumDescriptionLength;

    public static string? Normalize(string? value) => string.IsNullOrWhiteSpace(value) ? null : value.Trim();
}
