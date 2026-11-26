namespace SciFiLarpNetwork.Backend.Application.Items;

/// <param name="PageCount">The number of pages; at least 1, so an empty result still has one empty page.</param>
public sealed record ItemPage(IReadOnlyList<ItemSummary> Items, int PageNumber, int PageCount, long TotalCount);
