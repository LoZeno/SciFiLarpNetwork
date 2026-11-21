namespace SciFiLarpNetwork.Backend.Application.LarpEvents;

/// <param name="ParticipantCount">The number of characters registered for this day.</param>
/// <param name="Experience">The experience the day can give when the event closes; <see langword="null"/> for none.</param>
public sealed record LarpEventDaySummary(Guid DayId, DateOnly Date, int? Experience, int ParticipantCount);
