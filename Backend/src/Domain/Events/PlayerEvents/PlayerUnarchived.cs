namespace SciFiLarpNetwork.Backend.Domain.Events.PlayerEvents;

public sealed record PlayerUnarchived(Guid PlayerId, DateTimeOffset OccurredAtUtc);
