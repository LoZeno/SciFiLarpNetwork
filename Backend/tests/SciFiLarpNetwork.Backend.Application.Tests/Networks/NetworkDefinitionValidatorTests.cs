using SciFiLarpNetwork.Backend.Application.Networks;
using SciFiLarpNetwork.Backend.Domain.Networks;

namespace SciFiLarpNetwork.Backend.Tests.Application.Networks;

public sealed class NetworkDefinitionValidatorTests
{
    public static TheoryData<NetworkDefinition, NetworkOutcome> InvalidDefinitions => new()
    {
        { Definition(" "), NetworkOutcome.InvalidName },
        { Definition("Outer Rim") with { Description = "a\0b" }, NetworkOutcome.InvalidDescription },
        { Definition("Outer Rim") with { Description = "a\0b", Theme = (NetworkTheme)99 }, NetworkOutcome.InvalidDescription },
        { Definition("Outer Rim") with { Theme = (NetworkTheme)99 }, NetworkOutcome.InvalidTheme },
        { Definition(" ") with { Theme = (NetworkTheme)99 }, NetworkOutcome.InvalidName }
    };

    [Theory]
    [MemberData(nameof(InvalidDefinitions))]
    public void InvalidDefinitionIsRejectedWithItsFirstFailedCheck(NetworkDefinition definition, NetworkOutcome expected) =>
        Assert.Equal(expected, NetworkDefinitionValidator.Validate(definition));

    [Fact]
    public void NamedDefinitionWithAKnownThemeIsValid() =>
        Assert.Null(NetworkDefinitionValidator.Validate(Definition("Outer Rim")));

    [Theory]
    [InlineData("")]
    [InlineData("-rim")]
    [InlineData("outer rim")]
    public void MalformedCodeIsRejectedAtCreation(string code) =>
        Assert.Equal(NetworkOutcome.InvalidCode, NetworkDefinitionValidator.ValidateCreation(code, Definition("Outer Rim")));

    [Fact]
    public void InvalidDetailsAreReportedBeforeAMalformedCode() =>
        Assert.Equal(NetworkOutcome.InvalidName, NetworkDefinitionValidator.ValidateCreation("outer rim", Definition(" ")));

    [Fact]
    public void WellFormedCodeAndDetailsAreValidForCreation() =>
        Assert.Null(NetworkDefinitionValidator.ValidateCreation("rim", Definition("Outer Rim")));

    private static NetworkDefinition Definition(string name) => new(name, null, NetworkTheme.Blue);
}
