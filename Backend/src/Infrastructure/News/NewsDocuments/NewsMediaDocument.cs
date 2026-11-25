using SciFiLarpNetwork.Backend.Domain.News;

namespace SciFiLarpNetwork.Backend.Infrastructure.News.NewsDocuments;

/// <summary>The facts of an uploaded file; the bytes are in the media folder, named by <see cref="Id"/>.</summary>
/// <param name="UploadedBy">The recorded actor of the upload: an administrator account identifier.</param>
public sealed record NewsMediaDocument(
    Guid Id,
    string Name,
    string MediaType,
    NewsMediaKind Kind,
    long ByteCount,
    int? Width,
    int? Height,
    DateTimeOffset UploadedAtUtc,
    string? UploadedBy,
    bool IsRemoved)
{
    /// <summary>The media stream version, set by Marten for aggregate projection documents.</summary>
    public long Version { get; set; }
}
