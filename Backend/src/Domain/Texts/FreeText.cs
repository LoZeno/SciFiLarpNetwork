namespace SciFiLarpNetwork.Backend.Domain.Texts;

/// <summary>Rules shared by multi-line texts typed by an administrator, such as descriptions, notes and message bodies.</summary>
public static class FreeText
{
    /// <summary>
    /// A blank or missing text is storable. Line breaks and tabs are allowed, but U+0000 is refused, because PostgreSQL
    /// <c>jsonb</c> cannot store it.
    /// </summary>
    public static bool IsStorable(string? text) => text is null || !text.Contains('\0', StringComparison.Ordinal);
}
