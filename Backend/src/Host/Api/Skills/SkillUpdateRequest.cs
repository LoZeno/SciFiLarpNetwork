using System.ComponentModel.DataAnnotations;
using System.Text.Json.Serialization;
using SciFiLarpNetwork.Backend.Application.Skills;
using SciFiLarpNetwork.Backend.Domain.Skills;

namespace SciFiLarpNetwork.Backend.Api.Skills;

/// <param name="Version">The skill version the edit is based on; a stale version is rejected.</param>
/// <param name="PrerequisiteGroups">Requirement groups: every group must be met, by owning any one of its skills.</param>
[JsonUnmappedMemberHandling(JsonUnmappedMemberHandling.Disallow)]
public sealed record SkillUpdateRequest(
    [Range(1, long.MaxValue)] long Version,
    [Required] string Name,
    [StringLength(SkillCatalogLimits.MaximumDescriptionLength)] string? Description,
    int Cost,
    bool IsMultiPurchase,
    bool IsAdvanced,
    Guid[]? TalentIds,
    GameFlag[]? GameFlags,
    Guid[]? CardFlagIds,
    string[]? NewCardFlagNames,
    Guid[][]? PrerequisiteGroups)
{
    public SkillDefinition ToDefinition() =>
        new(
            Name,
            Description,
            Cost,
            IsMultiPurchase,
            IsAdvanced,
            TalentIds ?? [],
            GameFlags ?? [],
            CardFlagIds ?? [],
            NewCardFlagNames ?? [],
            PrerequisiteGroups ?? []);
}
