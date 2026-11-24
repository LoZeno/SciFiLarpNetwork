using SciFiLarpNetwork.Backend.Domain.News;

namespace SciFiLarpNetwork.Backend.Application.News;

/// <param name="Status">Compared with the current time.</param>
/// <param name="Media">The library media the body uses, in the order they first appear.</param>
/// <param name="SavedBy">The user name of the administrator who last saved the story, or the recorded actor.</param>
/// <param name="Version">The story version an edit must be based on.</param>
public sealed record NewsStoryDetails(
    Guid StoryId,
    NewsNetwork Network,
    NewsCharacter Author,
    string Title,
    string Body,
    DateTimeOffset StartsAtUtc,
    DateTimeOffset EndsAtUtc,
    int HackingDifficulty,
    NewsStoryStatus Status,
    IReadOnlyList<NewsMediaSummary> Media,
    DateTimeOffset SavedAtUtc,
    string? SavedBy,
    long Version);
