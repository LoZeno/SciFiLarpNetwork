namespace SciFiLarpNetwork.Management.ViewModels.Players;

/// <param name="DisplayName">
/// The running number and name, the player's name in search results, and localized status and archived tags.
/// </param>
public sealed record CharacterListItem(
    Guid CharacterId,
    Guid PlayerId,
    string Name,
    bool IsActive,
    bool IsArchived,
    string Href,
    string DisplayName);
