namespace SciFiLarpNetwork.Backend.Domain.Events.ItemTypeEvents;

public sealed record ItemTypeArchived(Guid ItemTypeId, DateTimeOffset OccurredAtUtc);
