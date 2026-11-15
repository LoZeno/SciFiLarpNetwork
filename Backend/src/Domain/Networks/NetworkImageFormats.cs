namespace SciFiLarpNetwork.Backend.Domain.Networks;

/// <summary>
/// Accepted network background image formats. The format is read from the file's leading bytes, never from a
/// client-supplied name or media type, so SVG and other formats are rejected.
/// </summary>
public static class NetworkImageFormats
{
    public const int MaximumByteCount = 2 * 1024 * 1024;

    private static readonly byte[] PngSignature = [0x89, 0x50, 0x4E, 0x47, 0x0D, 0x0A, 0x1A, 0x0A];
    private static readonly byte[] JpegSignature = [0xFF, 0xD8, 0xFF];
    private static readonly byte[] RiffSignature = "RIFF"u8.ToArray();
    private static readonly byte[] WebpSignature = "WEBP"u8.ToArray();

    /// <summary>Returns the media type of a PNG, JPEG, or WebP image, or <see langword="null"/> for anything else.</summary>
    public static string? DetectMediaType(ReadOnlySpan<byte> content) =>
        content.StartsWith(PngSignature)
            ? "image/png"
            : content.StartsWith(JpegSignature)
                ? "image/jpeg"
                : content.Length >= 12 && content.StartsWith(RiffSignature) &&
                  content[8..12].SequenceEqual(WebpSignature)
                    ? "image/webp"
                    : null;
}
