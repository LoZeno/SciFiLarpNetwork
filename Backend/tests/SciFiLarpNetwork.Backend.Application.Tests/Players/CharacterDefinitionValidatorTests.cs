using SciFiLarpNetwork.Backend.Application.Players;
using SciFiLarpNetwork.Backend.Domain.Characters;

namespace SciFiLarpNetwork.Backend.Tests.Application.Players;

public sealed class CharacterDefinitionValidatorTests
{
    public static TheoryData<CharacterDefinition, RosterOutcome> InvalidDefinitions => new()
    {
        { Named(" "), RosterOutcome.InvalidName },
        { Named("Kira") with { Title = new string('t', 65) }, RosterOutcome.InvalidCharacterText },
        { Named("Kira") with { Species = new string('s', 65) }, RosterOutcome.InvalidCharacterText },
        { Named("Kira") with { Gender = new string('g', 65) }, RosterOutcome.InvalidCharacterText },
        { Named("Kira") with { Type = new string('y', 65) }, RosterOutcome.InvalidCharacterText },
        { Named("Kira") with { ExperiencePoints = -1 }, RosterOutcome.InvalidExperiencePoints },
        { Named("Kira") with { Status = (CharacterStatus)99 }, RosterOutcome.InvalidCharacterStatus },
        { Named("Kira") with { Status = CharacterStatus.Dead }, RosterOutcome.DeathDateRequired },
        { Named(" ") with { ExperiencePoints = -1 }, RosterOutcome.InvalidName }
    };

    public static TheoryData<CharacterDefinition> ValidDefinitions => new()
    {
        Named("Kira"),
        Named("Kira") with { Status = CharacterStatus.Retired },
        Named("Kira") with { Status = CharacterStatus.Dead, DeathDate = new DateOnly(2026, 11, 1) }
    };

    public static TheoryData<CharacterDefinition, RosterOutcome> InvalidCreations => new()
    {
        { Named("Kira") with { Status = CharacterStatus.Retired }, RosterOutcome.InvalidCharacterStatus },
        { Named("Kira") with { Status = CharacterStatus.Dead, DeathDate = new DateOnly(2026, 11, 1) }, RosterOutcome.InvalidCharacterStatus },
        { Named("Kira") with { Status = CharacterStatus.Dead }, RosterOutcome.DeathDateRequired },
        { Named(" ") with { Status = CharacterStatus.Retired }, RosterOutcome.InvalidName }
    };

    public static TheoryData<NewCharacterStat, RosterOutcome> InvalidNewStats => new()
    {
        { new NewCharacterStat(" ", CharacterStatType.Number, 1, null), RosterOutcome.InvalidStatName },
        { new NewCharacterStat("Rank", (CharacterStatType)9, 1, null), RosterOutcome.InvalidStatType },
        { new NewCharacterStat("Rank", CharacterStatType.Number, -1, null), RosterOutcome.InvalidStatValue },
        { new NewCharacterStat("Rank", CharacterStatType.Number, null, "high"), RosterOutcome.InvalidStatValue },
        { new NewCharacterStat("Rank", CharacterStatType.Text, null, new string('x', 65)), RosterOutcome.InvalidStatValue }
    };

    [Theory]
    [MemberData(nameof(InvalidDefinitions))]
    public void InvalidDefinitionIsRejectedWithItsFirstFailedCheck(CharacterDefinition definition, RosterOutcome expected) =>
        Assert.Equal(expected, CharacterDefinitionValidator.Validate(definition));

    [Theory]
    [MemberData(nameof(ValidDefinitions))]
    public void DefinitionInAnyStatusWithItsRequiredDetailsIsValid(CharacterDefinition definition) =>
        Assert.Null(CharacterDefinitionValidator.Validate(definition));

    [Theory]
    [MemberData(nameof(InvalidCreations))]
    public void NewCharacterMustBeValidAndActive(CharacterDefinition definition, RosterOutcome expected) =>
        Assert.Equal(expected, CharacterDefinitionValidator.ValidateCreation(definition));

    [Fact]
    public void ActiveDefinitionIsValidForCreation() =>
        Assert.Null(CharacterDefinitionValidator.ValidateCreation(Named("Kira")));

    [Theory]
    [MemberData(nameof(InvalidNewStats))]
    public void InvalidNewStatIsRejected(NewCharacterStat newStat, RosterOutcome expected) =>
        Assert.Equal(expected, CharacterDefinitionValidator.ValidateNewStats([newStat]));

    [Fact]
    public void NewStatsOfEachTypeAreValid() =>
        Assert.Null(CharacterDefinitionValidator.ValidateNewStats(
        [
            new NewCharacterStat("Rank", CharacterStatType.Number, 3, null),
            new NewCharacterStat("Homeworld", CharacterStatType.Text, null, "Corellia")
        ]));

    private static CharacterDefinition Named(string name) =>
        new(name, null, null, null, null, 0, CharacterStatus.Active, null, null, [], []);
}
