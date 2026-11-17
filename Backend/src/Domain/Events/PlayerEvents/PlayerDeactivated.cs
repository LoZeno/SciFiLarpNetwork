namespace SciFiLarpNetwork.Backend.Domain.Events.PlayerEvents;

public sealed record PlayerDeactivated(Guid PlayerId, DateTimeOffset OccurredAtUtc);
