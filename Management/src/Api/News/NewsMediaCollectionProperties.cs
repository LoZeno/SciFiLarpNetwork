namespace SciFiLarpNetwork.Management.Api.News;

internal sealed record NewsMediaCollectionProperties(
    string Title,
    string Description,
    long MaximumPictureByteCount,
    long MaximumVideoByteCount,
    int MaximumPixelDimension,
    int MaximumNameLength,
    IReadOnlyList<string> PictureMediaTypes,
    IReadOnlyList<string> VideoMediaTypes);
