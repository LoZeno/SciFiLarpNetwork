namespace SciFiLarpNetwork.Backend.Domain.Events.ItemEvents;

public sealed record ItemUnarchived(Guid ItemId, DateTimeOffset OccurredAtUtc);
