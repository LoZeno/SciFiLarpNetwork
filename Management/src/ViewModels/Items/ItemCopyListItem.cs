namespace SciFiLarpNetwork.Management.ViewModels.Items;

/// <param name="DisplayName">The QR code and its state.</param>
public sealed record ItemCopyListItem(Guid CopyId, Guid PrintId, string QrCode, bool IsActive, string DisplayName);
