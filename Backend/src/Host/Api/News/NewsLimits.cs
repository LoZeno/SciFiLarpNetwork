using SciFiLarpNetwork.Backend.Domain.News;

namespace SciFiLarpNetwork.Backend.Api.News;

public static class NewsLimits
{
    /// <summary>Room for the largest video plus the form's name field and boundaries.</summary>
    public const long MaximumUploadRequestBytes = NewsMediaFormats.MaximumByteCount + (1024 * 1024);

    public static readonly IReadOnlyList<string> PictureMediaTypes = ["image/png", "image/jpeg", "image/webp", "image/gif"];

    public static readonly IReadOnlyList<string> VideoMediaTypes = ["video/webm", "video/mp4"];
}
