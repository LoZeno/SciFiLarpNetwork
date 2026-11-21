namespace SciFiLarpNetwork.Management.Api.LarpEvents;

internal sealed record ParticipantProperties(
    Guid CharacterId,
    int CharacterNumber,
    string CharacterName,
    Guid PlayerId,
    string PlayerDisplayName,
    string Status,
    bool IsArchived,
    int ExperiencePoints,
    IReadOnlyList<Guid> DayIds,
    IReadOnlyList<DateOnly> Dates,
    long ExperienceTotal,
    IReadOnlyList<ExperienceChangeProperties> ExperienceChanges);
