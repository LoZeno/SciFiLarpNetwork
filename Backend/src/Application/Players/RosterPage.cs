namespace SciFiLarpNetwork.Backend.Application.Players;

/// <param name="PageCount">The number of pages; at least 1, so an empty result still has one empty page.</param>
public sealed record RosterPage<T>(IReadOnlyList<T> Items, int PageNumber, int PageCount, long TotalCount);
