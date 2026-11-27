namespace SciFiLarpNetwork.Management.ViewModels.Items;

/// <param name="DisplayName">The name, with a localized tag for archived entries.</param>
public sealed record ItemCatalogEntryListItem(Guid Id, string Name, bool IsArchived, string Href, string DisplayName);
