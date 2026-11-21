namespace SciFiLarpNetwork.Backend.Application.LarpEvents;

/// <param name="ParticipantCount">The number of characters registered for this day.</param>
public sealed record LarpEventDaySummary(Guid DayId, DateOnly Date, int ParticipantCount);
