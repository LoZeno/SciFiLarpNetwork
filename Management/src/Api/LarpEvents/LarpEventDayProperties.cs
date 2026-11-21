namespace SciFiLarpNetwork.Management.Api.LarpEvents;

internal sealed record LarpEventDayProperties(Guid DayId, DateOnly Date, int ParticipantCount);
