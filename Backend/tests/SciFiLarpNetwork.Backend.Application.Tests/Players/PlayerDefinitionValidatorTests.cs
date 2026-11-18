using SciFiLarpNetwork.Backend.Application.Players;
using SciFiLarpNetwork.Backend.Domain.Players;

namespace SciFiLarpNetwork.Backend.Tests.Application.Players;

public sealed class PlayerDefinitionValidatorTests
{
    public static TheoryData<PlayerDefinition, RosterOutcome> InvalidDefinitions => new()
    {
        { Definition(" "), RosterOutcome.InvalidName },
        { Definition(new string('a', 65)), RosterOutcome.InvalidName },
        { Definition("Ann") with { Email = "not an email" }, RosterOutcome.InvalidEmail },
        { Definition("Ann") with { Phone = new string('1', 33) }, RosterOutcome.InvalidPhone },
        { Definition("Ann") with { Notes = new string('n', 2001) }, RosterOutcome.InvalidNotes },
        { Definition("Ann") with { Type = (PlayerType)99 }, RosterOutcome.InvalidPlayerType },
        { Definition(" ") with { Email = "not an email" }, RosterOutcome.InvalidName }
    };

    [Theory]
    [MemberData(nameof(InvalidDefinitions))]
    public void InvalidDefinitionIsRejectedWithItsFirstFailedCheck(PlayerDefinition definition, RosterOutcome expected) =>
        Assert.Equal(expected, PlayerDefinitionValidator.Validate(definition));

    [Fact]
    public void CompleteDefinitionIsValid() =>
        Assert.Null(PlayerDefinitionValidator.Validate(
            new PlayerDefinition("Ann Example", "ann@example.org", "555 0100", "Brings props", PlayerType.GameMaster)));

    private static PlayerDefinition Definition(string name) => new(name, null, null, null, PlayerType.Player);
}
