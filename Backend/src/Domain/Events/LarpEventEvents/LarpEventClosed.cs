namespace SciFiLarpNetwork.Backend.Domain.Events.LarpEventEvents;

public sealed record LarpEventClosed(Guid EventId, DateTimeOffset OccurredAtUtc);
