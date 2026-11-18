using SciFiLarpNetwork.Backend.Domain.Characters;
using SciFiLarpNetwork.Backend.Domain.CharacterSkills;

namespace SciFiLarpNetwork.Backend.Application.Players;

/// <summary>
/// Checks the rules that need no stored data. Returns <see langword="null"/> when the input is valid; otherwise the
/// outcome of the first failed check.
/// </summary>
public static class CharacterSkillsDefinitionValidator
{
    public static RosterOutcome? Validate(CharacterSkillsDefinition definition) =>
        definition.TalentIds.Distinct().Count() != definition.TalentIds.Count ? RosterOutcome.DuplicateTalent
        : definition.TalentIds.Count > CharacterSkillRules.MaxTalents ? RosterOutcome.TooManyTalents
        : definition.Purchases.DistinctBy(purchase => purchase.SkillId).Count() != definition.Purchases.Count
            ? RosterOutcome.DuplicateSkillPurchase
        : definition.Purchases.Any(purchase => purchase.Count is < 1 or > CharacterSkillRules.MaxPurchaseCount)
            ? RosterOutcome.InvalidPurchaseCount
        : !definition.Purchases.All(purchase => CharacterTexts.IsValidOptional(purchase.Specifics))
            ? RosterOutcome.InvalidSpecifics
        : null;
}
