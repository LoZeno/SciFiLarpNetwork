namespace SciFiLarpNetwork.Backend.Domain.Events.ItemTypeEvents;

public sealed record ItemTypeUnarchived(Guid ItemTypeId, DateTimeOffset OccurredAtUtc);
