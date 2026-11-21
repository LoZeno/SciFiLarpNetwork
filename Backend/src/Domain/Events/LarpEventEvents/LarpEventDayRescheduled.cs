namespace SciFiLarpNetwork.Backend.Domain.Events.LarpEventEvents;

/// <summary>A day moves to another date; registrations for the day stay with it.</summary>
public sealed record LarpEventDayRescheduled(Guid EventId, Guid DayId, DateOnly Date, DateTimeOffset OccurredAtUtc);
