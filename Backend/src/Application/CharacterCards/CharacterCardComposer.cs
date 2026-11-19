using System.Globalization;
using SciFiLarpNetwork.Backend.Application.Players;
using SciFiLarpNetwork.Backend.Application.Skills;
using SciFiLarpNetwork.Backend.Domain.Characters;

namespace SciFiLarpNetwork.Backend.Application.CharacterCards;

/// <summary>Applies the card content rules to a character's current data.</summary>
public static class CharacterCardComposer
{
    /// <param name="skillCatalog">Every skill, archived or not; it supplies the advanced and card flags.</param>
    /// <param name="cardFlags">Every card flag, in name order.</param>
    public static CharacterCard Compose(
        PlayerSummary player,
        CharacterSummary character,
        CharacterSkillsSummary skills,
        IReadOnlyList<SkillSummary> skillCatalog,
        IReadOnlyList<CardFlagSummary> cardFlags)
    {
        var catalog = skillCatalog.ToDictionary(skill => skill.SkillId);
        return new CharacterCard(
            player.Number,
            player.DisplayName,
            character.Number,
            character.Name,
            character.Title,
            character.Gender,
            character.Species,
            character.Type,
            character.QrIdentifier,
            character.ExperiencePoints,
            skills.FreePoints,
            character.IsArchived,
            CardFlagValues(skills.Skills, catalog, cardFlags),
            StatValues(character.Stats),
            Talents(skills, catalog));
    }

    private static CardFlagValue[] CardFlagValues(
        IReadOnlyList<CharacterSkillPurchaseSummary> purchases,
        Dictionary<Guid, SkillSummary> catalog,
        IReadOnlyList<CardFlagSummary> cardFlags) =>
        cardFlags
            .Select(cardFlag => new CardFlagValue(
                cardFlag.Name,
                purchases
                    .Where(purchase => catalog.TryGetValue(purchase.SkillId, out var skill) &&
                                       skill.CardFlagIds.Contains(cardFlag.CardFlagId))
                    .Sum(purchase => purchase.Count)))
            .ToArray();

    private static CardStatValue[] StatValues(IReadOnlyList<CharacterStatValueSummary> stats) =>
        stats
            .Where(stat => !stat.IsArchived)
            .Select(stat => new CardStatValue(stat.Name, PrintedValue(stat)))
            .Where(stat => stat.Value.Length > 0)
            .ToArray();

    private static string PrintedValue(CharacterStatValueSummary stat) =>
        stat.Type == CharacterStatType.Number
            ? stat.Number?.ToString(CultureInfo.InvariantCulture) ?? string.Empty
            : stat.Text ?? string.Empty;

    private static CardTalent[] Talents(CharacterSkillsSummary skills, Dictionary<Guid, SkillSummary> catalog) =>
        skills.Talents
            .Select(talent => new CardTalent(
                talent.Name,
                skills.Skills
                    .Where(purchase => purchase.TalentId == talent.TalentId)
                    .Select(purchase => new CardSkill(
                        purchase.Name,
                        purchase.Count,
                        catalog.TryGetValue(purchase.SkillId, out var skill) && skill.IsAdvanced))
                    .ToArray()))
            .ToArray();
}
