namespace SciFiLarpNetwork.Backend.Domain.Events.ItemCopyEvents;

/// <summary>Starts a copy's stream. The copy identifier, written as 32 hexadecimal digits, is its printed QR code.</summary>
public sealed record ItemCopyPrinted(Guid CopyId, Guid ItemId, Guid PrintId, DateTimeOffset OccurredAtUtc);
