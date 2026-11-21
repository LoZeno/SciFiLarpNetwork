namespace SciFiLarpNetwork.Backend.Domain.Events.LarpEventEvents;

/// <summary>A day gets another date or experience value; its registrations stay with it.</summary>
/// <param name="Experience">The experience points the day can give when the event closes; <see langword="null"/> for none.</param>
public sealed record LarpEventDayChanged(
    Guid EventId,
    Guid DayId,
    DateOnly Date,
    int? Experience,
    DateTimeOffset OccurredAtUtc);
