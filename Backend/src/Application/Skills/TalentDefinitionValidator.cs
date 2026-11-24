using SciFiLarpNetwork.Backend.Domain.Skills;

namespace SciFiLarpNetwork.Backend.Application.Skills;

public static class TalentDefinitionValidator
{
    /// <returns><see cref="CatalogOutcome.Succeeded"/> when the name and description are valid; otherwise the outcome
    /// of the first failed check.</returns>
    public static CatalogOutcome Validate(TalentDefinition definition) =>
        !CatalogNames.IsValid(definition.Name) ? CatalogOutcome.InvalidName
        : !CatalogTexts.IsValidDescription(definition.Description) ? CatalogOutcome.InvalidDescription
        : CatalogOutcome.Succeeded;
}
