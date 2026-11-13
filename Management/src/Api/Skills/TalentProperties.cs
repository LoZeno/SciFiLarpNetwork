namespace SciFiLarpNetwork.Management.Api.Skills;

internal sealed record TalentProperties(
    Guid TalentId,
    string Name,
    string? Description,
    bool IsArchived,
    long Version);
