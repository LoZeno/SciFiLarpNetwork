using SciFiLarpNetwork.Backend.Domain.CharacterSkills;

namespace SciFiLarpNetwork.Backend.Application.Players;

/// <param name="TalentIds">Every talent of the character, in the order they are shown.</param>
/// <param name="Purchases">Every skill the character owns; each skill at most once.</param>
public sealed record CharacterSkillsDefinition(IReadOnlyList<Guid> TalentIds, IReadOnlyList<SkillPurchase> Purchases);
