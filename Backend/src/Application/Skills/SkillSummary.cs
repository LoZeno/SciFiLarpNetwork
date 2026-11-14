using SciFiLarpNetwork.Backend.Domain.Skills;

namespace SciFiLarpNetwork.Backend.Application.Skills;

public sealed record SkillSummary(
    Guid SkillId,
    string Name,
    string? Description,
    int Cost,
    bool IsMultiPurchase,
    bool IsAdvanced,
    IReadOnlyList<Guid> TalentIds,
    IReadOnlyList<GameFlag> GameFlags,
    IReadOnlyList<Guid> CardFlagIds,
    IReadOnlyList<IReadOnlyList<Guid>> PrerequisiteGroups,
    bool IsArchived,
    long Version);
