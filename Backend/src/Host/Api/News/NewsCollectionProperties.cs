namespace SciFiLarpNetwork.Backend.Api.News;

/// <param name="NetworkId">The network whose stories are listed; <see langword="null"/> lists nothing.</param>
/// <param name="ActiveAt">Lists only the stories active at this time; <see langword="null"/> for all of them.</param>
/// <param name="MaximumMediaPerStory">The most pictures and videos one body can place.</param>
public sealed record NewsCollectionProperties(
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
