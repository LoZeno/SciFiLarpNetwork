using SciFiLarpNetwork.Backend.Application.CharacterCards;
using SciFiLarpNetwork.Backend.Application.Players;
using SciFiLarpNetwork.Backend.Application.Skills;
using SciFiLarpNetwork.Backend.Domain.Characters;
using SciFiLarpNetwork.Backend.Domain.Players;

namespace SciFiLarpNetwork.Backend.Tests.Application.CharacterCards;

public sealed class CharacterCardComposerTests
{
    private static readonly Guid PlayerId = Guid.NewGuid();
    private static readonly Guid CharacterId = Guid.NewGuid();
    private static readonly Guid CombatId = Guid.NewGuid();
    private static readonly Guid ScienceId = Guid.NewGuid();
    private static readonly Guid ToughnessFlagId = Guid.NewGuid();
    private static readonly Guid FocusFlagId = Guid.NewGuid();

    [Fact]
    public void FrontAndPointsComeFromThePlayerCharacterAndSkills()
    {
        var card = CharacterCardComposer.Compose(
            Player(),
            Character() with { Title = "Captain", Gender = "Female", Species = "Human", Type = "Pilot" },
            Skills(freePoints: 15),
            [],
            []);

        Assert.Equal(
            new CharacterCard(12, "Ann", 34, "Kira", "Captain", "Female", "Human", "Pilot", "qr-code", 120, 15, false,
                Array.Empty<CardFlagValue>(), Array.Empty<CardStatValue>(), Array.Empty<CardTalent>()),
            card with
            {
                CardFlags = Array.Empty<CardFlagValue>(),
                Stats = Array.Empty<CardStatValue>(),
                Talents = Array.Empty<CardTalent>()
            });
    }

    [Fact]
    public void ArchivedCharacterIsMarkedArchived()
    {
        var card = CharacterCardComposer.Compose(Player(), Character() with { IsArchived = true }, Skills(), [], []);

        Assert.True(card.IsArchived);
    }

    [Fact]
    public void CardFlagValueCountsEveryPurchaseOfEveryOwnedSkillWithTheFlag()
    {
        var tough = Skill("Tough", isMultiPurchase: true, cardFlagIds: [ToughnessFlagId]);
        var hardy = Skill("Hardy", cardFlagIds: [ToughnessFlagId]);

        var card = CharacterCardComposer.Compose(
            Player(),
            Character(),
            Skills(purchases: [Purchase(tough, CombatId, count: 3), Purchase(hardy, CombatId)]),
            [tough, hardy],
            [new CardFlagSummary(ToughnessFlagId, "Toughness")]);

        Assert.Equal([new CardFlagValue("Toughness", 4)], card.CardFlags);
    }

    [Fact]
    public void CardFlagWithoutOwnedSkillsIsPrintedWithZero()
    {
        var card = CharacterCardComposer.Compose(
            Player(),
            Character(),
            Skills(),
            [],
            [new CardFlagSummary(FocusFlagId, "Focus"), new CardFlagSummary(ToughnessFlagId, "Toughness")]);

        Assert.Equal([new CardFlagValue("Focus", 0), new CardFlagValue("Toughness", 0)], card.CardFlags);
    }

    [Fact]
    public void ArchivedOwnedSkillStillCountsTowardItsCardFlags()
    {
        var hardy = Skill("Hardy", cardFlagIds: [ToughnessFlagId], isArchived: true);

        var card = CharacterCardComposer.Compose(
            Player(),
            Character(),
            Skills(purchases: [Purchase(hardy, CombatId)]),
            [hardy],
            [new CardFlagSummary(ToughnessFlagId, "Toughness")]);

        Assert.Equal([new CardFlagValue("Toughness", 1)], card.CardFlags);
    }

    [Fact]
    public void StatsLeaveOutArchivedStatsAndEmptyTextValues()
    {
        var card = CharacterCardComposer.Compose(
            Player(),
            Character() with
            {
                Stats =
                [
                    Stat("Dark Side", CharacterStatType.Number, number: 5),
                    Stat("Old score", CharacterStatType.Number, number: 9, isArchived: true),
                    Stat("Rank", CharacterStatType.Text, text: ""),
                    Stat("Teaching", CharacterStatType.Text, text: "adept")
                ]
            },
            Skills(),
            [],
            []);

        Assert.Equal([new CardStatValue("Dark Side", "5"), new CardStatValue("Teaching", "adept")], card.Stats);
    }

    [Fact]
    public void SkillsAreGroupedByTalentInPickedOrderWithCountsAndTheAdvancedMark()
    {
        var blaster = Skill("Blaster");
        var sniper = Skill("Sniper", isAdvanced: true);
        var tough = Skill("Tough", isMultiPurchase: true);
        var medicine = Skill("Medicine");

        var card = CharacterCardComposer.Compose(
            Player(),
            Character(),
            Skills(
                talents: [Talent(ScienceId, "Science"), Talent(CombatId, "Combat")],
                purchases:
                [
                    Purchase(blaster, CombatId), Purchase(medicine, ScienceId), Purchase(sniper, CombatId),
                    Purchase(tough, CombatId, count: 3)
                ]),
            [blaster, sniper, tough, medicine],
            []);

        Assert.Equal(
            [
                ("Science", new[] { new CardSkill("Medicine", 1, false) }),
                ("Combat", [new CardSkill("Blaster", 1, false), new CardSkill("Sniper", 1, true), new CardSkill("Tough", 3, false)])
            ],
            card.Talents.Select(talent => (talent.Name, talent.Skills.ToArray())).ToArray(),
            TalentComparer.Instance);
    }

    [Fact]
    public void TalentWithoutBoughtSkillsIsStillListed()
    {
        var card = CharacterCardComposer.Compose(Player(), Character(), Skills(talents: [Talent(CombatId, "Combat")]), [], []);

        Assert.Equal(["Combat"], card.Talents.Select(talent => talent.Name));
    }

    private static PlayerSummary Player() =>
        new(PlayerId, 12, "Ann", null, null, null, PlayerType.Player, PlayerStatus.Active, false, 1);

    private static CharacterSummary Character() =>
        new(CharacterId, 34, PlayerId, "Ann", "Kira", null, null, null, null, 120, CharacterStatus.Active, null, null,
            "qr-code", [], false, 1);

    private static CharacterSkillsSummary Skills(
        long freePoints = 0,
        IReadOnlyList<CharacterTalentSummary>? talents = null,
        IReadOnlyList<CharacterSkillPurchaseSummary>? purchases = null) =>
        new(CharacterId, "Kira", 120, freePoints, 5,
            talents ?? [Talent(CombatId, "Combat"), Talent(ScienceId, "Science")],
            purchases ?? [],
            1);

    private static CharacterTalentSummary Talent(Guid talentId, string name) => new(talentId, name, false);

    private static SkillSummary Skill(
        string name,
        bool isMultiPurchase = false,
        bool isAdvanced = false,
        IReadOnlyList<Guid>? cardFlagIds = null,
        bool isArchived = false) =>
        new(Guid.NewGuid(), name, null, 1, isMultiPurchase, isAdvanced, [CombatId, ScienceId], [], cardFlagIds ?? [], [],
            isArchived, 1);

    private static CharacterSkillPurchaseSummary Purchase(SkillSummary skill, Guid talentId, int count = 1) =>
        new(skill.SkillId, skill.Name, talentId, count, null, skill.Cost, skill.IsMultiPurchase, skill.IsArchived);

    private static CharacterStatValueSummary Stat(
        string name,
        CharacterStatType type,
        int? number = null,
        string? text = null,
        bool isArchived = false) =>
        new(Guid.NewGuid(), name, type, number, text, isArchived);

    private sealed class TalentComparer : IEqualityComparer<(string Name, CardSkill[] Skills)>
    {
        public static readonly TalentComparer Instance = new();

        public bool Equals((string Name, CardSkill[] Skills) x, (string Name, CardSkill[] Skills) y) =>
            x.Name == y.Name && x.Skills.SequenceEqual(y.Skills);

        public int GetHashCode((string Name, CardSkill[] Skills) obj) => obj.Name.GetHashCode(StringComparison.Ordinal);
    }
}
