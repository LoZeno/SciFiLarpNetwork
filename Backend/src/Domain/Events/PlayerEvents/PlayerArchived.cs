namespace SciFiLarpNetwork.Backend.Domain.Events.PlayerEvents;

public sealed record PlayerArchived(Guid PlayerId, DateTimeOffset OccurredAtUtc);
