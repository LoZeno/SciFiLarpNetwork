using SciFiLarpNetwork.Backend.Domain.Characters;

namespace SciFiLarpNetwork.Backend.Application.LarpEvents;

/// <summary>A character registered for an event, with the character's current details.</summary>
/// <param name="Dates">The registered days' dates, in date order.</param>
/// <param name="ExperienceTotal">The sum of every experience change given at this event.</param>
/// <param name="ExperienceChanges">The experience changes given at this event, oldest first.</param>
public sealed record ParticipantSummary(
    Guid CharacterId,
    int CharacterNumber,
    string CharacterName,
    Guid PlayerId,
    string PlayerDisplayName,
    CharacterStatus Status,
    bool IsArchived,
    int ExperiencePoints,
    IReadOnlyList<Guid> DayIds,
    IReadOnlyList<DateOnly> Dates,
    long ExperienceTotal,
    IReadOnlyList<ExperienceChangeSummary> ExperienceChanges);
