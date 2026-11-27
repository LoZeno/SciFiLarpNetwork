namespace SciFiLarpNetwork.Backend.Domain.Events.RarityEvents;

public sealed record RarityRenamed(Guid RarityId, string Name, string NormalizedName, DateTimeOffset OccurredAtUtc);
