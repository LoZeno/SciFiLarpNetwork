namespace SciFiLarpNetwork.Backend.Domain.Events.ItemEvents;

/// <summary>
/// Appended to the single item-composition stream whenever a component is added to an item, so concurrent additions
/// conflict on that stream's version and cannot together make an item contain itself.
/// </summary>
public sealed record ItemCompositionChanged(Guid ItemId, DateTimeOffset OccurredAtUtc);
