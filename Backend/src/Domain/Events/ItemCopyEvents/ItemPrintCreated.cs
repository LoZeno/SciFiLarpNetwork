namespace SciFiLarpNetwork.Backend.Domain.Events.ItemCopyEvents;

/// <summary>
/// Starts a print's stream, whose identifier the client chose, so a repeated print request collides with it.
/// </summary>
/// <param name="ItemIds">The printed items, in print order.</param>
/// <param name="CopyIds">The new copies, item by item in print order.</param>
public sealed record ItemPrintCreated(
    Guid PrintId,
    Guid[] ItemIds,
    int CopiesPerItem,
    Guid[] CopyIds,
    DateTimeOffset OccurredAtUtc);
