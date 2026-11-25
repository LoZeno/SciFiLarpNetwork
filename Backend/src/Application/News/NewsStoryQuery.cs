namespace SciFiLarpNetwork.Backend.Application.News;

/// <param name="NetworkId">The network whose stories are listed; <see langword="null"/> lists nothing.</param>
/// <param name="ActiveAt">Lists only the stories active at this time; <see langword="null"/> for all of them.</param>
/// <param name="PageNumber">The one-based page of <see cref="PageSize"/> stories, latest start first.</param>
public sealed record NewsStoryQuery(Guid? NetworkId, DateTimeOffset? ActiveAt, int PageNumber)
{
    public const int PageSize = 50;
}
