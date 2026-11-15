namespace SciFiLarpNetwork.Backend.Domain.Events.NetworkEvents;

public sealed record NetworkEnabled(Guid NetworkId, DateTimeOffset OccurredAtUtc);
