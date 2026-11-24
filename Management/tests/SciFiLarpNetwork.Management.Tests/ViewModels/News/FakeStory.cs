namespace SciFiLarpNetwork.Management.Tests.ViewModels.News;

internal sealed record FakeStory(
    Guid Id,
    Guid NetworkId,
    Guid AuthorId,
    string Title,
    string Body,
    DateTimeOffset StartsAt,
    DateTimeOffset EndsAt,
    int HackingDifficulty,
    long Version);
