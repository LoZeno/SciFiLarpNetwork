using SciFiLarpNetwork.Backend.Application.LarpEvents;

namespace SciFiLarpNetwork.Backend.Tests.Application.LarpEvents;

public sealed class LarpEventDefinitionValidatorTests
{
    public static TheoryData<LarpEventDefinition, LarpEventOutcome> InvalidDefinitions => new()
    {
        { new LarpEventDefinition(" ", null), LarpEventOutcome.InvalidName },
        { new LarpEventDefinition(new string('n', 65), null), LarpEventOutcome.InvalidName },
        { new LarpEventDefinition("Fort\u0007", null), LarpEventOutcome.InvalidName },
        { new LarpEventDefinition("Fort", new string('d', 2001)), LarpEventOutcome.InvalidDescription },
        { new LarpEventDefinition(" ", new string('d', 2001)), LarpEventOutcome.InvalidName }
    };

    [Theory]
    [MemberData(nameof(InvalidDefinitions))]
    public void InvalidDefinitionIsRejectedWithItsFirstFailedCheck(LarpEventDefinition definition, LarpEventOutcome expected) =>
        Assert.Equal(expected, LarpEventDefinitionValidator.Validate(definition));

    [Fact]
    public void CompleteDefinitionIsValid() =>
        Assert.Null(LarpEventDefinitionValidator.Validate(new LarpEventDefinition(new string('n', 64), new string('d', 2000))));
}
