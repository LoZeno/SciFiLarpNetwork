using SciFiLarpNetwork.Backend.Domain.News;

namespace SciFiLarpNetwork.Backend.Application.News;

/// <param name="Width">The picture's width in pixels; <see langword="null"/> for a video.</param>
/// <param name="Height">The picture's height in pixels; <see langword="null"/> for a video.</param>
/// <param name="UploadedBy">The user name of the uploading administrator, or the recorded actor.</param>
public sealed record NewsMediaSummary(
    Guid MediaId,
    string Name,
    string MediaType,
    NewsMediaKind Kind,
    long ByteCount,
    int? Width,
    int? Height,
    DateTimeOffset UploadedAtUtc,
    string? UploadedBy);
