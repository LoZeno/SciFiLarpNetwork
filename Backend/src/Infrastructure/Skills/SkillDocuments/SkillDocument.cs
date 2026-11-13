using SciFiLarpNetwork.Backend.Domain.Skills;

namespace SciFiLarpNetwork.Backend.Infrastructure.Skills.SkillDocuments;

public sealed record SkillDocument(
    Guid Id,
    string Name,
    string NormalizedName,
    string? Description,
    int Cost,
    bool IsMultiPurchase,
    bool IsAdvanced,
    Guid[] TalentIds,
    GameFlag[] GameFlags,
    Guid[] CardFlagIds,
    Guid[][] PrerequisiteGroups,
    bool IsArchived)
{
    /// <summary>The skill stream version, set by Marten for aggregate projection documents.</summary>
    public long Version { get; set; }
}
