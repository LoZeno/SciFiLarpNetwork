namespace SciFiLarpNetwork.Management.ViewModels.Players;

/// <param name="DisplayName">The name and localized type, with a tag for archived stats.</param>
public sealed record CharacterStatListItem(Guid StatId, string Name, bool IsArchived, string Href, string DisplayName);
