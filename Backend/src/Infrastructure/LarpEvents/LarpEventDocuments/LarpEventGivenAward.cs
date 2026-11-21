namespace SciFiLarpNetwork.Backend.Infrastructure.LarpEvents.LarpEventDocuments;

/// <param name="BaseExperience">Whether the event's base experience was given.</param>
/// <param name="DayIds">The days whose experience was given.</param>
public sealed record LarpEventGivenAward(Guid CharacterId, bool BaseExperience, Guid[] DayIds);
