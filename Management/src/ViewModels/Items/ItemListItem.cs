namespace SciFiLarpNetwork.Management.ViewModels.Items;

/// <param name="DisplayName">The name, with a localized tag for archived items.</param>
public sealed record ItemListItem(Guid ItemId, string Name, bool IsArchived, string Href, string DisplayName);
