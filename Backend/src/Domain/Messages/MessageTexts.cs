using SciFiLarpNetwork.Backend.Domain.Texts;

namespace SciFiLarpNetwork.Backend.Domain.Messages;

/// <summary>Rules for a message's title, body, and recipient list.</summary>
public static class MessageTexts
{
    public const int MaximumTitleLength = 128;
    public const int MaximumBodyLength = 10000;
    public const int MaximumRecipients = 200;

    /// <summary>A title is 1–128 characters after trimming, without control characters.</summary>
    public static bool IsValidTitle(string? title) =>
        !string.IsNullOrWhiteSpace(title) && title.Trim() is var trimmed &&
        trimmed.Length <= MaximumTitleLength && !trimmed.Any(char.IsControl);

    /// <summary>
    /// A body is stored exactly as typed: up to 10,000 characters, and not only whitespace. Line breaks and tabs are
    /// kept, but U+0000 is refused, because PostgreSQL <c>jsonb</c> cannot store it.
    /// </summary>
    public static bool IsValidBody(string? body) =>
        !string.IsNullOrWhiteSpace(body) && body.Length <= MaximumBodyLength && FreeText.IsStorable(body);
}
