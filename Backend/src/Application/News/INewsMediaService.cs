namespace SciFiLarpNetwork.Backend.Application.News;

/// <summary>The shared library of pictures and videos that news bodies place.</summary>
public interface INewsMediaService
{
    /// <summary>Every media item not removed, newest upload first.</summary>
    Task<IReadOnlyList<NewsMediaSummary>> GetMediaAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Stores a picture (PNG, JPEG, WebP, GIF; up to 5 MB and 4096 × 4096 pixels) or a video (WebM, MP4; up to 100 MB).
    /// The type is detected from the content.
    /// </summary>
    Task<NewsResult<NewsMediaSummary>> UploadMediaAsync(string? name, Stream content,
        CancellationToken cancellationToken = default);

    /// <summary>Removes media that no story uses, and deletes its file.</summary>
    Task<NewsOutcome> RemoveMediaAsync(Guid mediaId, CancellationToken cancellationToken = default);

    /// <summary>Opens the stored file of media not removed, or returns <see langword="null"/>.</summary>
    Task<NewsMediaContent?> OpenMediaAsync(Guid mediaId, CancellationToken cancellationToken = default);
}
