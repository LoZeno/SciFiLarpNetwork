namespace SciFiLarpNetwork.Management.Api.News;

/// <param name="Status"><c>scheduled</c>, <c>active</c>, or <c>expired</c>, compared with the server's time.</param>
internal sealed record NewsStorySummaryProperties(
    Guid StoryId,
    Guid NetworkId,
    NewsCharacterProperties Author,
    string Title,
    DateTimeOffset StartsAtUtc,
    DateTimeOffset EndsAtUtc,
    string Status);
