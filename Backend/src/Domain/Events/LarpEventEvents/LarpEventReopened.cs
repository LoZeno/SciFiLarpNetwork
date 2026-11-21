namespace SciFiLarpNetwork.Backend.Domain.Events.LarpEventEvents;

public sealed record LarpEventReopened(Guid EventId, DateTimeOffset OccurredAtUtc);
