using SciFiLarpNetwork.Backend.Domain.Players;
using SciFiLarpNetwork.Backend.Domain.Skills;

namespace SciFiLarpNetwork.Backend.Application.Players;

public static class PlayerDefinitionValidator
{
    /// <returns><see langword="null"/> when the definition is valid; otherwise the outcome of the first failed check.</returns>
    public static RosterOutcome? Validate(PlayerDefinition definition) =>
        !CatalogNames.IsValid(definition.DisplayName) ? RosterOutcome.InvalidName
        : !PlayerContacts.IsValidEmail(definition.Email) ? RosterOutcome.InvalidEmail
        : !PlayerContacts.IsValidPhone(definition.Phone) ? RosterOutcome.InvalidPhone
        : !PlayerContacts.IsValidNotes(definition.Notes) ? RosterOutcome.InvalidNotes
        : !Enum.IsDefined(definition.Type) ? RosterOutcome.InvalidPlayerType
        : null;
}
