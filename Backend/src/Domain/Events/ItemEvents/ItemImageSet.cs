namespace SciFiLarpNetwork.Backend.Domain.Events.ItemEvents;

/// <summary>Records the item's picture by reference; the bytes are stored in the media folder.</summary>
public sealed record ItemImageSet(
    Guid ItemId,
    Guid ImageId,
    string MediaType,
    int ByteCount,
    DateTimeOffset OccurredAtUtc);
