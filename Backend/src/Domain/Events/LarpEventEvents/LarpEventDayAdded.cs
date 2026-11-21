namespace SciFiLarpNetwork.Backend.Domain.Events.LarpEventEvents;

/// <param name="Experience">The experience points the day can give when the event closes; <see langword="null"/> for none.</param>
public sealed record LarpEventDayAdded(
    Guid EventId,
    Guid DayId,
    DateOnly Date,
    int? Experience,
    DateTimeOffset OccurredAtUtc);
