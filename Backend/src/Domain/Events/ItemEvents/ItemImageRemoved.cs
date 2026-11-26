namespace SciFiLarpNetwork.Backend.Domain.Events.ItemEvents;

public sealed record ItemImageRemoved(Guid ItemId, DateTimeOffset OccurredAtUtc);
