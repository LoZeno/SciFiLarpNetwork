namespace SciFiLarpNetwork.Backend.Infrastructure.LarpEvents;

/// <summary>The checked experience one participant gets when the event closes.</summary>
internal sealed record ClosingGrant(Guid CharacterId, bool BaseExperience, Guid[] DayIds, int Amount);
