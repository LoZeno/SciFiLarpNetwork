namespace SciFiLarpNetwork.Backend.Domain.CharacterSkills;

/// <summary>The catalog data of a skill that the character skill rules depend on.</summary>
public sealed record SkillFacts(
    Guid SkillId,
    int Cost,
    bool IsMultiPurchase,
    bool IsArchived,
    IReadOnlyList<Guid> TalentIds,
    IReadOnlyList<IReadOnlyList<Guid>> PrerequisiteGroups);
