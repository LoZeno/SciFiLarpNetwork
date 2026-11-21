namespace SciFiLarpNetwork.Management.Api.LarpEvents;

internal sealed record LarpEventProperties(
    Guid EventId,
    int Number,
    string Name,
    string? Description,
    string Status,
    bool IsArchived,
    int? BaseExperience,
    DateOnly? FirstDate,
    DateOnly? LastDate,
    IReadOnlyList<LarpEventDayProperties> Days,
    int ParticipantCount,
    long Version);
