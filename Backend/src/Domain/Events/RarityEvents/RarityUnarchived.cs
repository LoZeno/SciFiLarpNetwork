namespace SciFiLarpNetwork.Backend.Domain.Events.RarityEvents;

public sealed record RarityUnarchived(Guid RarityId, DateTimeOffset OccurredAtUtc);
