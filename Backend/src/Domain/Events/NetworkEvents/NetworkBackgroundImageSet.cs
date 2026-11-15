namespace SciFiLarpNetwork.Backend.Domain.Events.NetworkEvents;

/// <summary>Records the uploaded image by reference; the image bytes are stored outside the event stream.</summary>
public sealed record NetworkBackgroundImageSet(
    Guid NetworkId,
    Guid ImageId,
    string MediaType,
    int ByteCount,
    DateTimeOffset OccurredAtUtc);
