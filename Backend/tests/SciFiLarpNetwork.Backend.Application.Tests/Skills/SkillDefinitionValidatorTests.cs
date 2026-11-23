using SciFiLarpNetwork.Backend.Application.Skills;

namespace SciFiLarpNetwork.Backend.Tests.Application.Skills;

public sealed class SkillDefinitionValidatorTests
{
    public static TheoryData<SkillDefinition, CatalogOutcome> InvalidDefinitions => new()
    {
        { Definition(" ", cost: 1), CatalogOutcome.InvalidName },
        { Definition("Hacking I", cost: 1) with { Description = "a\0b" }, CatalogOutcome.InvalidDescription },
        { Definition("Hacking I", cost: -1) with { Description = "a\0b" }, CatalogOutcome.InvalidDescription },
        { Definition("Hacking I", cost: -1), CatalogOutcome.InvalidCost },
        { Definition("Hacking I", cost: 1000), CatalogOutcome.InvalidCost },
        { Definition(" ", cost: -1), CatalogOutcome.InvalidName }
    };

    [Theory]
    [MemberData(nameof(InvalidDefinitions))]
    public void InvalidDetailsAreRejectedWithTheirFirstFailedCheck(SkillDefinition definition, CatalogOutcome expected) =>
        Assert.Equal(expected, SkillDefinitionValidator.ValidateDetails(definition));

    [Theory]
    [InlineData(0)]
    [InlineData(999)]
    public void CostFromZeroTo999IsValid(int cost) =>
        Assert.Equal(CatalogOutcome.Succeeded, SkillDefinitionValidator.ValidateDetails(Definition("Hacking I", cost)));

    private static SkillDefinition Definition(string name, int cost) =>
        new(name, null, cost, false, false, [], [], [], [], []);
}
