namespace SciFiLarpNetwork.Backend.Domain.Events.PlayerEvents;

public sealed record PlayerActivated(Guid PlayerId, DateTimeOffset OccurredAtUtc);
