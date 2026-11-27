namespace SciFiLarpNetwork.Backend.Domain.Events.RarityEvents;

public sealed record RarityArchived(Guid RarityId, DateTimeOffset OccurredAtUtc);
