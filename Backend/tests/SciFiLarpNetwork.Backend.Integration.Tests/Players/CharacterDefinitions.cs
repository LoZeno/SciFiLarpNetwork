using SciFiLarpNetwork.Backend.Application.Players;
using SciFiLarpNetwork.Backend.Domain.Characters;

namespace SciFiLarpNetwork.Backend.Tests.Integration.Players;

internal static class CharacterDefinitions
{
    public static CharacterDefinition Named(string name) =>
        new(name, null, null, null, null, 0, CharacterStatus.Active, null, null, [], []);

    /// <summary>The definition that saves the character unchanged.</summary>
    public static CharacterDefinition From(CharacterSummary character) =>
        new(
            character.Name,
            character.Title,
            character.Species,
            character.Gender,
            character.Type,
            character.ExperiencePoints,
            character.Status,
            character.DeathDate,
            character.NetworkId,
            [],
            []);
}
