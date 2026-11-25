namespace SciFiLarpNetwork.Backend.Application.News;

/// <param name="Body">The Markdown body, stored exactly as typed.</param>
/// <param name="StartsAt">Converted to UTC when stored.</param>
/// <param name="EndsAt">Converted to UTC when stored; must be after <paramref name="StartsAt"/>.</param>
public sealed record NewsStoryContent(
    Guid NetworkId,
    Guid AuthorId,
    string? Title,
    string? Body,
    DateTimeOffset StartsAt,
    DateTimeOffset EndsAt,
    int HackingDifficulty);
