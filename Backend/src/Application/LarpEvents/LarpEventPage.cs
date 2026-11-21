namespace SciFiLarpNetwork.Backend.Application.LarpEvents;

/// <param name="PageCount">The number of pages; at least 1, so an empty result still has one empty page.</param>
public sealed record LarpEventPage(IReadOnlyList<LarpEventSummary> Items, int PageNumber, int PageCount, long TotalCount);
