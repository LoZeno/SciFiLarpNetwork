namespace SciFiLarpNetwork.Backend.Domain.Events.CharacterEvents;

/// <summary>An experience change given at an event (MANAGER-03).</summary>
/// <param name="ExperiencePoints">The character's experience points after the change.</param>
public sealed record CharacterExperienceChanged(
    Guid CharacterId,
    Guid EventId,
    int Amount,
    int ExperiencePoints,
    DateTimeOffset OccurredAtUtc);
