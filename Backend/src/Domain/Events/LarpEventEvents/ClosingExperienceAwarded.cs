namespace SciFiLarpNetwork.Backend.Domain.Events.LarpEventEvents;

/// <summary>
/// The experience a participant got when the event closed. The character's stream gets the matching
/// <see cref="CharacterEvents.CharacterExperienceChanged"/> in the same commit. What was given is never given again
/// when the event closes once more after a reopen.
/// </summary>
/// <param name="BaseExperience">Whether the event's base experience was given.</param>
/// <param name="DayIds">The days whose experience was given.</param>
/// <param name="Amount">The total experience given.</param>
public sealed record ClosingExperienceAwarded(
    Guid EventId,
    Guid CharacterId,
    bool BaseExperience,
    IReadOnlyList<Guid> DayIds,
    int Amount,
    DateTimeOffset OccurredAtUtc);
