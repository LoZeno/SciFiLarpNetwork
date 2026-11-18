using SciFiLarpNetwork.Backend.Application.Players;
using SciFiLarpNetwork.Backend.Domain.CharacterSkills;

namespace SciFiLarpNetwork.Backend.Tests.Application.Players;

public sealed class CharacterSkillsDefinitionValidatorTests
{
    private static readonly Guid Talent = Guid.NewGuid();
    private static readonly Guid Skill = Guid.NewGuid();

    public static TheoryData<CharacterSkillsDefinition, RosterOutcome> InvalidDefinitions => new()
    {
        { Definition(talents: 6), RosterOutcome.TooManyTalents },
        { Definition(purchases: [Buy(Skill), Buy(Skill)]), RosterOutcome.DuplicateSkillPurchase },
        { Definition(purchases: [Buy(Skill, count: 0)]), RosterOutcome.InvalidPurchaseCount },
        { Definition(purchases: [Buy(Skill, count: 1000)]), RosterOutcome.InvalidPurchaseCount },
        { Definition(purchases: [Buy(Skill, specifics: new string('s', 65))]), RosterOutcome.InvalidSpecifics },
        { Definition(talents: 6, purchases: [Buy(Skill, count: 0)]), RosterOutcome.TooManyTalents },
        { new CharacterSkillsDefinition([Talent, Talent], []), RosterOutcome.DuplicateTalent },
        { new CharacterSkillsDefinition([Talent, Talent, Talent, Talent, Talent, Talent], []), RosterOutcome.DuplicateTalent }
    };

    public static TheoryData<CharacterSkillsDefinition> ValidDefinitions => new()
    {
        Definition(),
        Definition(talents: 5),
        Definition(purchases: [Buy(Skill, count: 999, specifics: new string('s', 64))]),
        Definition(purchases: [Buy(Skill, specifics: "   ")])
    };

    [Theory]
    [MemberData(nameof(InvalidDefinitions))]
    public void InvalidDefinitionIsRejected(CharacterSkillsDefinition definition, RosterOutcome expected) =>
        Assert.Equal(expected, CharacterSkillsDefinitionValidator.Validate(definition));

    [Theory]
    [MemberData(nameof(ValidDefinitions))]
    public void ValidDefinitionIsAccepted(CharacterSkillsDefinition definition) =>
        Assert.Null(CharacterSkillsDefinitionValidator.Validate(definition));

    private static CharacterSkillsDefinition Definition(int talents = 1, IReadOnlyList<SkillPurchase>? purchases = null) =>
        new([.. Enumerable.Range(0, talents).Select(_ => Guid.NewGuid())], purchases ?? []);

    private static SkillPurchase Buy(Guid skillId, int count = 1, string? specifics = null) =>
        new(skillId, Talent, count, specifics);
}
