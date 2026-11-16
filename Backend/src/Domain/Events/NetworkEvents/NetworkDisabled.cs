namespace SciFiLarpNetwork.Backend.Domain.Events.NetworkEvents;

public sealed record NetworkDisabled(Guid NetworkId, DateTimeOffset OccurredAtUtc);
