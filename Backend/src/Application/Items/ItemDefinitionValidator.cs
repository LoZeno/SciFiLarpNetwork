using SciFiLarpNetwork.Backend.Domain.Items;
using SciFiLarpNetwork.Backend.Domain.Skills;

namespace SciFiLarpNetwork.Backend.Application.Items;

public static class ItemDefinitionValidator
{
    /// <returns><see cref="ItemOutcome.Succeeded"/> when every typed detail is valid; otherwise the outcome of the first
    /// failed check. Whether the type and rarity exist is checked against the catalog.</returns>
    public static ItemOutcome Validate(ItemDefinition definition) =>
        !CatalogNames.IsValid(definition.Name) ? ItemOutcome.InvalidName
        : !ItemTexts.IsValidText(definition.Description) ? ItemOutcome.InvalidDescription
        : !ItemTexts.IsValidText(definition.Effect) ? ItemOutcome.InvalidEffect
        : !ItemTexts.IsValidCost(definition.Cost) ? ItemOutcome.InvalidCost
        : !ItemTexts.IsValidCharges(definition.Charges) ? ItemOutcome.InvalidCharges
        : ItemOutcome.Succeeded;
}
