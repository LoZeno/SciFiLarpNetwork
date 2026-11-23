using SciFiLarpNetwork.Backend.Domain.Networks;
using SciFiLarpNetwork.Backend.Domain.Skills;
using SciFiLarpNetwork.Backend.Domain.Texts;

namespace SciFiLarpNetwork.Backend.Application.Networks;

/// <summary>
/// Every method returns <see langword="null"/> when its input is valid; otherwise the outcome of the first failed check.
/// </summary>
public static class NetworkDefinitionValidator
{
    public static NetworkOutcome? Validate(NetworkDefinition definition) =>
        !CatalogNames.IsValid(definition.Name) ? NetworkOutcome.InvalidName
        : !FreeText.IsStorable(definition.Description) ? NetworkOutcome.InvalidDescription
        : !Enum.IsDefined(definition.Theme) ? NetworkOutcome.InvalidTheme
        : null;

    /// <summary>Like <see cref="Validate"/>, and also checks the code, which is set only at creation.</summary>
    public static NetworkOutcome? ValidateCreation(string code, NetworkDefinition definition) =>
        Validate(definition) ?? (!NetworkCodes.IsValid(code) ? NetworkOutcome.InvalidCode : null);
}
