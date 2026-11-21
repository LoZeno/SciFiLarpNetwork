namespace SciFiLarpNetwork.Backend.Domain.Events.LarpEventEvents;

public sealed record LarpEventDayAdded(Guid EventId, Guid DayId, DateOnly Date, DateTimeOffset OccurredAtUtc);
