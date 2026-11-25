namespace SciFiLarpNetwork.Management.Api.News;

internal sealed record NewsCollectionProperties(
    string Title,
    string Description,
    Guid? NetworkId,
    DateTimeOffset? ActiveAt,
    int PageNumber,
    int PageCount,
    long TotalCount,
    int MaximumTitleLength,
    int MaximumBodyLength,
    int MaximumMediaPerStory,
    int MinimumHackingDifficulty);
