namespace SciFiLarpNetwork.Backend.Domain.Events.NetworkEvents;

public sealed record NetworkBackgroundImageRemoved(Guid NetworkId, DateTimeOffset OccurredAtUtc);
