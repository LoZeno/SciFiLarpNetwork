using SciFiLarpNetwork.Management.Api.Messages;
using SciFiLarpNetwork.Management.ViewModels.LarpEvents;

namespace SciFiLarpNetwork.Management.ViewModels.Messages;

/// <summary>How the message screen and dialog name a sender or recipient.</summary>
internal static class MessageCharacterNames
{
    /// <summary>
    /// The running number and current name, with the localized archived tag; a character that cannot be found is
    /// named as unknown.
    /// </summary>
    public static string Describe(MessageCharacterProperties character, Func<string, string> translate) =>
        !character.IsKnown
            ? translate("messages.unknownCharacter")
            : character.IsArchived
                ? $"#{character.Number} {character.Name} {translate("characters.archivedTag")}"
                : $"#{character.Number} {character.Name}";

    public static CharacterChoice Choice(MessageCharacterProperties character, Func<string, string> translate) =>
        new(character.CharacterId, Describe(character, translate));
}
