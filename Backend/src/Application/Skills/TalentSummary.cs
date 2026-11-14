namespace SciFiLarpNetwork.Backend.Application.Skills;

public sealed record TalentSummary(
    Guid TalentId,
    string Name,
    string? Description,
    bool IsArchived,
    long Version);
