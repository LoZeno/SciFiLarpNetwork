namespace SciFiLarpNetwork.Backend.Application.Items;

/// <summary>Everything printed on one item card, which is one copy of the item.</summary>
/// <param name="QrCode">The copy's QR code, or a sample code for a preview.</param>
/// <param name="IsArchived">Whether the item is archived; reprints of archived items are allowed.</param>
public sealed record ItemCard(
    string ItemType,
    string Name,
    string QrCode,
    string? Description,
    string? Effect,
    int? Charges,
    DateOnly? ExpiresOn,
    bool IsArchived);
