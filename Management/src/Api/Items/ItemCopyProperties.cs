namespace SciFiLarpNetwork.Management.Api.Items;

internal sealed record ItemCopyProperties(
    Guid CopyId,
    string QrCode,
    Guid ItemId,
    Guid PrintId,
    DateTimeOffset PrintedAtUtc,
    string? PrintedBy,
    bool IsActive,
    DateTimeOffset? ChangedAtUtc,
    string? ChangedBy);
