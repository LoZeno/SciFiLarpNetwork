namespace SciFiLarpNetwork.Management.ViewModels.Items;

/// <param name="DisplayName">The quantity and name, with a localized tag for archived components.</param>
public sealed record ItemComponentListItem(Guid ItemId, string Name, int Quantity, bool IsArchived, string DisplayName);
