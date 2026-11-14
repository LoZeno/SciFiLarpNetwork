using SciFiLarpNetwork.Backend.Domain.Skills;

namespace SciFiLarpNetwork.Backend.Application.Skills;

/// <summary>
/// The complete desired state of a skill for a create or edit command.
/// </summary>
/// <param name="NewCardFlagNames">Card flag names typed by the administrator. A name that matches an existing
/// card flag (ignoring case) reuses it; any other name defines a new card flag.</param>
/// <param name="PrerequisiteGroups">Requirement groups: every group must be met, by owning any one of its skills.</param>
public sealed record SkillDefinition(
    string Name,
    string? Description,
    int Cost,
    bool IsMultiPurchase,
    bool IsAdvanced,
    IReadOnlyList<Guid> TalentIds,
    IReadOnlyList<GameFlag> GameFlags,
    IReadOnlyList<Guid> CardFlagIds,
    IReadOnlyList<string> NewCardFlagNames,
    IReadOnlyList<IReadOnlyList<Guid>> PrerequisiteGroups);
