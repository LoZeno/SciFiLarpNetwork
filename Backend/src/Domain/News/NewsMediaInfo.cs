namespace SciFiLarpNetwork.Backend.Domain.News;

/// <param name="Width">The picture's width in pixels; <see langword="null"/> for a video.</param>
/// <param name="Height">The picture's height in pixels; <see langword="null"/> for a video.</param>
public sealed record NewsMediaInfo(string MediaType, NewsMediaKind Kind, int? Width, int? Height);
