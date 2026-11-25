namespace SciFiLarpNetwork.Backend.Infrastructure.News.NewsDocuments;

/// <param name="MediaIds">The library media the body uses, each once, in the order they first appear.</param>
/// <param name="SavedBy">The recorded actor of the last save: an administrator account identifier.</param>
public sealed record NewsStoryDocument(
    Guid Id,
    Guid NetworkId,
    Guid AuthorId,
    string Title,
    string Body,
    DateTimeOffset StartsAtUtc,
    DateTimeOffset EndsAtUtc,
    int HackingDifficulty,
    Guid[] MediaIds,
    DateTimeOffset SavedAtUtc,
    string? SavedBy)
{
    /// <summary>The story stream version, set by Marten for aggregate projection documents.</summary>
    public long Version { get; set; }
}
