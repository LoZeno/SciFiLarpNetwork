namespace SciFiLarpNetwork.Backend.Application.Messages;

/// <param name="PageCount">The number of pages; at least 1, so an empty result still has one empty page.</param>
public sealed record MessagePage(IReadOnlyList<MessageSummary> Items, int PageNumber, int PageCount, long TotalCount);
