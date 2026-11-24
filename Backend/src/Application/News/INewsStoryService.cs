namespace SciFiLarpNetwork.Backend.Application.News;

/// <summary>Network news stories, as administrators list, create, and edit them.</summary>
public interface INewsStoryService
{
    /// <summary>Lists one network's stories, latest start first, optionally only those active at a time.</summary>
    Task<NewsStoryPage> GetStoriesAsync(NewsStoryQuery query, CancellationToken cancellationToken = default);

    Task<NewsStoryDetails?> GetStoryAsync(Guid storyId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Creates a story. Creating the same <paramref name="storyId"/> again with the same content returns the story
    /// already created; with different content it reports <see cref="NewsOutcome.StoryIdInUse"/>.
    /// </summary>
    Task<NewsResult<NewsStoryDetails>> CreateStoryAsync(Guid storyId, NewsStoryContent content,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Replaces every value of the story, if it is still at <paramref name="expectedVersion"/>; otherwise reports
    /// <see cref="NewsOutcome.ConcurrentChange"/>.
    /// </summary>
    Task<NewsResult<NewsStoryDetails>> EditStoryAsync(Guid storyId, long expectedVersion, NewsStoryContent content,
        CancellationToken cancellationToken = default);
}
