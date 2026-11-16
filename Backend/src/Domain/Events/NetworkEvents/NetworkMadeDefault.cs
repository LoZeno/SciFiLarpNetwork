namespace SciFiLarpNetwork.Backend.Domain.Events.NetworkEvents;

public sealed record NetworkMadeDefault(Guid NetworkId, DateTimeOffset OccurredAtUtc);
