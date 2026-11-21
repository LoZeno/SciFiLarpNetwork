using SciFiLarpNetwork.Backend.Domain.LarpEvents;
using SciFiLarpNetwork.Backend.Domain.Skills;

namespace SciFiLarpNetwork.Backend.Application.LarpEvents;

public static class LarpEventDefinitionValidator
{
    /// <returns><see langword="null"/> when the definition is valid; otherwise the outcome of the first failed check.</returns>
    public static LarpEventOutcome? Validate(LarpEventDefinition definition) =>
        !CatalogNames.IsValid(definition.Name) ? LarpEventOutcome.InvalidName
        : !LarpEventTexts.IsValidDescription(definition.Description) ? LarpEventOutcome.InvalidDescription
        : null;
}
