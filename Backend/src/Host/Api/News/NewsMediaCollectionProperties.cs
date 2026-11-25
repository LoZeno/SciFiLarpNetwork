namespace SciFiLarpNetwork.Backend.Api.News;

/// <summary>The media library with its upload limits, which clients show where media is uploaded.</summary>
public sealed record NewsMediaCollectionProperties(
    string Title,
    string Description,
    long MaximumPictureByteCount,
    long MaximumVideoByteCount,
    int MaximumPixelDimension,
    int MaximumNameLength,
    IReadOnlyList<string> PictureMediaTypes,
    IReadOnlyList<string> VideoMediaTypes);
