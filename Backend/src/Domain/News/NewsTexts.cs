using SciFiLarpNetwork.Backend.Domain.Texts;

namespace SciFiLarpNetwork.Backend.Domain.News;

/// <summary>Rules for a news story's title, body, times, and hacking difficulty.</summary>
public static class NewsTexts
{
    public const int MaximumTitleLength = 128;
    public const int MaximumBodyLength = 10000;
    public const int MaximumMediaPerStory = 10;
    public const int MinimumHackingDifficulty = 1;
    public const int MaximumMediaNameLength = 128;

    /// <summary>A title is 1–128 characters after trimming, without control characters.</summary>
    public static bool IsValidTitle(string? title) =>
        !string.IsNullOrWhiteSpace(title) && title.Trim() is var trimmed &&
        trimmed.Length <= MaximumTitleLength && !trimmed.Any(char.IsControl);

    /// <summary>
    /// A body is Markdown stored exactly as typed: up to 10,000 characters, not only whitespace, and without U+0000,
    /// which PostgreSQL <c>jsonb</c> cannot store.
    /// </summary>
    public static bool IsValidBody(string? body) =>
        !string.IsNullOrWhiteSpace(body) && body.Length <= MaximumBodyLength && FreeText.IsStorable(body);

    /// <summary>A story is active from its start up to, but not including, its end, so the end must come later.</summary>
    public static bool AreValidTimes(DateTimeOffset startsAt, DateTimeOffset endsAt) => endsAt > startsAt;

    /// <summary>A whole number of at least 1; there is no maximum (SPEC-26).</summary>
    public static bool IsValidHackingDifficulty(int difficulty) => difficulty >= MinimumHackingDifficulty;

    /// <summary>A media name is 1–128 characters after trimming, without control characters.</summary>
    public static bool IsValidMediaName(string? name) =>
        !string.IsNullOrWhiteSpace(name) && name.Trim() is var trimmed &&
        trimmed.Length <= MaximumMediaNameLength && !trimmed.Any(char.IsControl);
}
