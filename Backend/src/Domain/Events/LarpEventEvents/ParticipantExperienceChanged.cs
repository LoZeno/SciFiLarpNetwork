namespace SciFiLarpNetwork.Backend.Domain.Events.LarpEventEvents;

/// <summary>
/// The event's record of an experience change. The character's stream gets the matching
/// <see cref="CharacterEvents.CharacterExperienceChanged"/> in the same commit; the administrator is the event's
/// user name metadata.
/// </summary>
public sealed record ParticipantExperienceChanged(
    Guid EventId,
    Guid CharacterId,
    int Amount,
    DateTimeOffset OccurredAtUtc);
