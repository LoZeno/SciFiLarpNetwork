namespace SciFiLarpNetwork.Backend.Application.LarpEvents;

/// <summary>The experience one participant gets when the event closes.</summary>
/// <param name="BaseExperience">Whether the character gets the event's base experience.</param>
/// <param name="DayIds">The registered days whose experience the character gets.</param>
public sealed record ClosingAward(Guid CharacterId, bool BaseExperience, IReadOnlyList<Guid> DayIds);
