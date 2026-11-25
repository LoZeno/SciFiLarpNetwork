namespace SciFiLarpNetwork.Management.Api.News;

/// <param name="Kind"><c>picture</c> or <c>video</c>.</param>
internal sealed record NewsMediaProperties(
    Guid MediaId,
    string Name,
    string MediaType,
    string Kind,
    long ByteCount,
    int? Width,
    int? Height,
    DateTimeOffset UploadedAtUtc,
    string? UploadedBy)
{
    public bool IsVideo => string.Equals(Kind, "video", StringComparison.OrdinalIgnoreCase);
}
