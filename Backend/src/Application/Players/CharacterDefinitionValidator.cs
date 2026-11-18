using SciFiLarpNetwork.Backend.Domain.Characters;
using SciFiLarpNetwork.Backend.Domain.Skills;

namespace SciFiLarpNetwork.Backend.Application.Players;

/// <summary>
/// Every method returns <see langword="null"/> when its input is valid; otherwise the outcome of the first failed check.
/// </summary>
public static class CharacterDefinitionValidator
{
    public static RosterOutcome? Validate(CharacterDefinition definition) =>
        !CatalogNames.IsValid(definition.Name) ? RosterOutcome.InvalidName
        : !new[] { definition.Title, definition.Species, definition.Gender, definition.Type }.All(CharacterTexts.IsValidOptional)
            ? RosterOutcome.InvalidCharacterText
        : definition.ExperiencePoints < 0 ? RosterOutcome.InvalidExperiencePoints
        : !Enum.IsDefined(definition.Status) ? RosterOutcome.InvalidCharacterStatus
        : definition is { Status: CharacterStatus.Dead, DeathDate: null } ? RosterOutcome.DeathDateRequired
        : null;

    /// <summary>Like <see cref="Validate"/>, and a new character must also be Active.</summary>
    public static RosterOutcome? ValidateCreation(CharacterDefinition definition) =>
        Validate(definition)
        ?? (definition.Status != CharacterStatus.Active ? RosterOutcome.InvalidCharacterStatus : null);

    /// <summary>
    /// Checks the stats typed by name. Values for existing stats depend on the stored stat types, so the service
    /// checks those first and calls this afterwards.
    /// </summary>
    public static RosterOutcome? ValidateNewStats(IReadOnlyList<NewCharacterStat> newStats) =>
        newStats.Any(stat => !CatalogNames.IsValid(stat.Name)) ? RosterOutcome.InvalidStatName
        : newStats.Any(stat => !Enum.IsDefined(stat.Type)) ? RosterOutcome.InvalidStatType
        : newStats.Any(stat => !CharacterStatValues.IsValid(stat.Type, stat.Number, stat.Text)) ? RosterOutcome.InvalidStatValue
        : null;
}
