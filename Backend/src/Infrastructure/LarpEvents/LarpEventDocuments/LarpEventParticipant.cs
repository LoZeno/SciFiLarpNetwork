namespace SciFiLarpNetwork.Backend.Infrastructure.LarpEvents.LarpEventDocuments;

/// <param name="DayIds">The registered days; never empty.</param>
public sealed record LarpEventParticipant(Guid CharacterId, Guid[] DayIds);
