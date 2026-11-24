using SciFiLarpNetwork.Backend.Domain.Skills;

namespace SciFiLarpNetwork.Backend.Application.Skills;

public static class SkillDefinitionValidator
{
    public const int MaximumCost = 999;

    /// <returns><see cref="CatalogOutcome.Succeeded"/> when the name, description and cost are valid; otherwise the
    /// outcome of the first failed check.</returns>
    public static CatalogOutcome ValidateDetails(SkillDefinition definition) =>
        !CatalogNames.IsValid(definition.Name) ? CatalogOutcome.InvalidName
        : !CatalogTexts.IsValidDescription(definition.Description) ? CatalogOutcome.InvalidDescription
        : definition.Cost is < 0 or > MaximumCost ? CatalogOutcome.InvalidCost
        : CatalogOutcome.Succeeded;
}
