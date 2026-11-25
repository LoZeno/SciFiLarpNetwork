using SciFiLarpNetwork.Backend.Domain.News;

namespace SciFiLarpNetwork.Backend.Domain.Events.NewsEvents;

/// <summary>Records the uploaded file's facts only; its bytes are kept in the media folder, named by the identifier.</summary>
/// <param name="Width">The picture's width in pixels; <see langword="null"/> for a video.</param>
/// <param name="Height">The picture's height in pixels; <see langword="null"/> for a video.</param>
public sealed record NewsMediaUploaded(
    Guid MediaId,
    string Name,
    string MediaType,
    NewsMediaKind Kind,
    long ByteCount,
    int? Width,
    int? Height,
    DateTimeOffset OccurredAtUtc);
