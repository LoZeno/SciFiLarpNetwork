namespace SciFiLarpNetwork.Backend.Domain.Events.ItemTypeEvents;

public sealed record ItemTypeRenamed(Guid ItemTypeId, string Name, string NormalizedName, DateTimeOffset OccurredAtUtc);
