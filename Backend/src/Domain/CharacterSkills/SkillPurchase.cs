namespace SciFiLarpNetwork.Backend.Domain.CharacterSkills;

/// <summary>A skill a character owns, bought through one of the character's talents.</summary>
/// <param name="Count">How many times the skill was bought; above 1 only for multi-purchase skills.</param>
/// <param name="Specifics">An optional free-text note, 1–64 characters after trimming when present.</param>
public sealed record SkillPurchase(Guid SkillId, Guid TalentId, int Count, string? Specifics);
