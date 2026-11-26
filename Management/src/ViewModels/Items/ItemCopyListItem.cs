namespace SciFiLarpNetwork.Management.ViewModels.Items;

/// <param name="DisplayName">The QR code, its state, and who printed it and when.</param>
public sealed record ItemCopyListItem(Guid CopyId, string QrCode, bool IsActive, string DisplayName);
