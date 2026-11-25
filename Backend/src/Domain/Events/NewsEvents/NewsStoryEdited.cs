namespace SciFiLarpNetwork.Backend.Domain.Events.NewsEvents;

/// <summary>Every value of the story after the edit, so history keeps the full story at each version.</summary>
/// <param name="Body">The Markdown body exactly as typed.</param>
/// <param name="MediaIds">The library media the body uses, each once, in the order they first appear.</param>
public sealed record NewsStoryEdited(
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
