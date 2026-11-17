namespace SciFiLarpNetwork.Management.ViewModels.Players;

/// <param name="DisplayName">The running number and name, with localized tags for inactive and archived players.</param>
public sealed record PlayerListItem(
    Guid PlayerId,
    string Name,
    bool IsActive,
    bool IsArchived,
    string Href,
    string DisplayName);
