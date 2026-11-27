namespace SciFiLarpNetwork.Management.ViewModels.Items;

/// <param name="DisplayName">When and by whom the print was made, and how many of its copies are active.</param>
public sealed record ItemPrintListItem(Guid PrintId, string DisplayName);
