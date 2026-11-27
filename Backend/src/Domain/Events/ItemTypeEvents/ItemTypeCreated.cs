namespace SciFiLarpNetwork.Backend.Domain.Events.ItemTypeEvents;

public sealed record ItemTypeCreated(Guid ItemTypeId, string Name, string NormalizedName, DateTimeOffset OccurredAtUtc);
