using SciFiLarpNetwork.Backend.Domain.News;

namespace SciFiLarpNetwork.Backend.Application.News;

/// <param name="Status">Compared with the current time.</param>
public sealed record NewsStorySummary(
    Guid StoryId,
    Guid NetworkId,
    NewsCharacter Author,
    string Title,
    DateTimeOffset StartsAtUtc,
    DateTimeOffset EndsAtUtc,
    NewsStoryStatus Status);
