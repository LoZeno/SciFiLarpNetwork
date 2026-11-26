namespace SciFiLarpNetwork.Backend.Domain.Events.RarityEvents;

public sealed record RarityCreated(Guid RarityId, string Name, string NormalizedName, DateTimeOffset OccurredAtUtc);
