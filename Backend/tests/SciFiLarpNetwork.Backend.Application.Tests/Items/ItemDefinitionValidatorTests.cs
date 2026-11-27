using SciFiLarpNetwork.Backend.Application.Items;

namespace SciFiLarpNetwork.Backend.Tests.Application.Items;

public sealed class ItemDefinitionValidatorTests
{
    private static readonly ItemDefinition Valid =
        new("Medkit", Guid.NewGuid(), Guid.NewGuid(), "A kit.", "Heals two wounds.", 10, 3, new DateOnly(2027, 1, 1));

    public static TheoryData<ItemDefinition, ItemOutcome> Cases => new()
    {
        { Valid, ItemOutcome.Succeeded },
        { Valid with { Description = null, Effect = null, Charges = null, ExpiresOn = null, Cost = 0 }, ItemOutcome.Succeeded },
        { Valid with { Name = "  " }, ItemOutcome.InvalidName },
        { Valid with { Name = new string('a', 65) }, ItemOutcome.InvalidName },
        { Valid with { Description = new string('a', 4001) }, ItemOutcome.InvalidDescription },
        { Valid with { Effect = "bad\0effect" }, ItemOutcome.InvalidEffect },
        { Valid with { Cost = -1 }, ItemOutcome.InvalidCost },
        { Valid with { Cost = 1_000_000 }, ItemOutcome.InvalidCost },
        { Valid with { Charges = 0 }, ItemOutcome.InvalidCharges },
        { Valid with { Charges = 1000 }, ItemOutcome.InvalidCharges }
    };

    [Theory]
    [MemberData(nameof(Cases))]
    public void ValidatesEveryTypedDetail(ItemDefinition definition, ItemOutcome expected)
    {
        Assert.Equal(expected, ItemDefinitionValidator.Validate(definition));
    }
}
