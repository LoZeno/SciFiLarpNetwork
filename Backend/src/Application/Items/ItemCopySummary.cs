namespace SciFiLarpNetwork.Backend.Application.Items;

/// <summary>One printed copy of an item.</summary>
/// <param name="QrCode">The copy's public QR code: its identifier as 32 lower-case hexadecimal digits.</param>
/// <param name="PrintedBy">The administrator who printed it, by user name when known.</param>
/// <param name="ChangedAtUtc">When the copy was last deactivated or turned back on, if ever.</param>
/// <param name="ChangedBy">Who last deactivated it or turned it back on.</param>
public sealed record ItemCopySummary(
    Guid CopyId,
    string QrCode,
    Guid ItemId,
    Guid PrintId,
    DateTimeOffset PrintedAtUtc,
    string? PrintedBy,
    bool IsActive,
    DateTimeOffset? ChangedAtUtc,
    string? ChangedBy);
