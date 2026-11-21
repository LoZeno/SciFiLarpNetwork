namespace SciFiLarpNetwork.Backend.Domain.Events.LarpEventEvents;

public sealed record LarpEventDayRemoved(Guid EventId, Guid DayId, DateTimeOffset OccurredAtUtc);
