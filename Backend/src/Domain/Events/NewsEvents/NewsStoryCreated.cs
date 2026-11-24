namespace SciFiLarpNetwork.Backend.Domain.Events.NewsEvents;

/// <param name="Body">The Markdown body exactly as typed.</param>
/// <param name="MediaIds">The library media the body uses, each once, in the order they first appear.</param>
public sealed record NewsStoryCreated(
    Guid StoryId,
    Guid NetworkId,
    Guid AuthorId,
    string Title,
    string Body,
    DateTimeOffset StartsAtUtc,
    DateTimeOffset EndsAtUtc,
    int HackingDifficulty,
    IReadOnlyList<Guid> MediaIds,
    DateTimeOffset OccurredAtUtc);
