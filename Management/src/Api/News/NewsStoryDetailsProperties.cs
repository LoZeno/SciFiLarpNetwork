namespace SciFiLarpNetwork.Management.Api.News;

/// <param name="Status"><c>scheduled</c>, <c>active</c>, or <c>expired</c>, compared with the server's time.</param>
/// <param name="Media">The library media the body uses.</param>
internal sealed record NewsStoryDetailsProperties(
    Guid StoryId,
    NewsNetworkProperties Network,
    NewsCharacterProperties Author,
    string Title,
    string Body,
    DateTimeOffset StartsAtUtc,
    DateTimeOffset EndsAtUtc,
    int HackingDifficulty,
    string Status,
    IReadOnlyList<NewsMediaProperties> Media,
    DateTimeOffset SavedAtUtc,
    string? SavedBy,
    long Version);
