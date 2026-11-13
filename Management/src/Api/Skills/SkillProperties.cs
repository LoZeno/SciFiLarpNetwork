namespace SciFiLarpNetwork.Management.Api.Skills;

internal sealed record SkillProperties(
    Guid SkillId,
    string Name,
    string? Description,
    int Cost,
    bool IsMultiPurchase,
    bool IsAdvanced,
    IReadOnlyList<Guid> TalentIds,
    IReadOnlyList<string> GameFlags,
    IReadOnlyList<Guid> CardFlagIds,
    IReadOnlyList<IReadOnlyList<Guid>> PrerequisiteGroups,
    bool IsArchived,
    long Version);
