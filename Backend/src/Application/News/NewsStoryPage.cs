namespace SciFiLarpNetwork.Backend.Application.News;

/// <param name="PageCount">The number of pages; at least 1, so an empty result still has one empty page.</param>
public sealed record NewsStoryPage(IReadOnlyList<NewsStorySummary> Items, int PageNumber, int PageCount, long TotalCount);
