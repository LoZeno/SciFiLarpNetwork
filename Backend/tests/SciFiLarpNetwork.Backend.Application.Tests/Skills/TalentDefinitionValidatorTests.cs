using SciFiLarpNetwork.Backend.Application.Skills;

namespace SciFiLarpNetwork.Backend.Tests.Application.Skills;

public sealed class TalentDefinitionValidatorTests
{
    public static TheoryData<TalentDefinition, CatalogOutcome> InvalidDefinitions => new()
    {
        { new TalentDefinition(" ", null), CatalogOutcome.InvalidName },
        { new TalentDefinition("Medicine", "a\0b"), CatalogOutcome.InvalidDescription },
        { new TalentDefinition("Medicine", new string('d', 2001)), CatalogOutcome.InvalidDescription },
        { new TalentDefinition(" ", "a\0b"), CatalogOutcome.InvalidName }
    };

    [Theory]
    [MemberData(nameof(InvalidDefinitions))]
    public void InvalidDefinitionIsRejectedWithItsFirstFailedCheck(TalentDefinition definition, CatalogOutcome expected) =>
        Assert.Equal(expected, TalentDefinitionValidator.Validate(definition));

    [Theory]
    [InlineData(null)]
    [InlineData("Healing arts:\r\n\tfield surgery")]
    public void NamedTalentWithoutOrWithMultiLineDescriptionIsValid(string? description) =>
        Assert.Equal(CatalogOutcome.Succeeded, TalentDefinitionValidator.Validate(new TalentDefinition("Medicine", description)));
}
