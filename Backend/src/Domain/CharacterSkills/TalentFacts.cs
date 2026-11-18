namespace SciFiLarpNetwork.Backend.Domain.CharacterSkills;

/// <summary>The catalog data of a talent that the character skill rules depend on.</summary>
public sealed record TalentFacts(Guid TalentId, bool IsArchived);
