namespace SciFiLarpNetwork.Backend.Domain.CharacterSkills;

/// <summary>A requested replacement of a character's talents and skills, with the catalog data it depends on.</summary>
/// <param name="Talents">At least every talent in <paramref name="TalentIds"/> that exists.</param>
/// <param name="Skills">At least every skill in <paramref name="CurrentPurchases"/> and <paramref name="Purchases"/> that exists.</param>
public sealed record CharacterSkillChange(
    int ExperiencePoints,
    IReadOnlyList<Guid> CurrentTalentIds,
    IReadOnlyList<SkillPurchase> CurrentPurchases,
    IReadOnlyList<Guid> TalentIds,
    IReadOnlyList<SkillPurchase> Purchases,
    IReadOnlyDictionary<Guid, TalentFacts> Talents,
    IReadOnlyDictionary<Guid, SkillFacts> Skills);
