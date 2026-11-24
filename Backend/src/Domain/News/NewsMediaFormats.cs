using System.Buffers.Binary;
using SciFiLarpNetwork.Backend.Domain.Networks;

namespace SciFiLarpNetwork.Backend.Domain.News;

/// <summary>
/// Accepted news pictures (PNG, JPEG, WebP, GIF) and videos (WebM, MP4). The type, and a picture's pixel size, are read
/// from the file's own first bytes, never from a name or a declared type, so SVG and every other format is refused.
/// </summary>
/// <remarks>Only the header is read; readers must treat media that fails to decode as missing.</remarks>
public static class NewsMediaFormats
{
    public const long MaximumPictureByteCount = 5L * 1024 * 1024;
    public const long MaximumVideoByteCount = 100L * 1024 * 1024;
    public const int MaximumPixelDimension = 4096;

    /// <summary>The largest file of any kind; uploads stop reading past it.</summary>
    public const long MaximumByteCount = MaximumVideoByteCount;

    private static readonly byte[] EbmlSignature = [0x1A, 0x45, 0xDF, 0xA3];
    private static readonly byte[] WebmDocType = [0x42, 0x82, 0x84, (byte)'w', (byte)'e', (byte)'b', (byte)'m'];

    private static readonly string[] Mp4Brands =
        ["isom", "iso2", "iso4", "iso5", "iso6", "mp41", "mp42", "avc1", "M4V ", "dash"];

    /// <summary>
    /// The type of a file from its first bytes: for a picture all of it, which is at most
    /// <see cref="MaximumPictureByteCount"/>; for a video the first few bytes are enough.
    /// </summary>
    /// <returns><see langword="null"/> for any other content or a header that cannot be read.</returns>
    public static NewsMediaInfo? Inspect(ReadOnlySpan<byte> start) =>
        NetworkImageFormats.Inspect(start) is { } image
            ? new NewsMediaInfo(image.MediaType, NewsMediaKind.Picture, image.Width, image.Height)
            : ReadGif(start) ?? ReadVideo(start);

    /// <summary>Whether a file of this kind may have <paramref name="byteCount"/> bytes.</summary>
    public static bool HasAcceptedSize(NewsMediaInfo media, long byteCount) =>
        byteCount <= (media.Kind == NewsMediaKind.Picture ? MaximumPictureByteCount : MaximumVideoByteCount);

    /// <summary>Whether a picture's width and height are both within <see cref="MaximumPixelDimension"/>; videos always are.</summary>
    public static bool HasAcceptedDimensions(NewsMediaInfo media) =>
        media.Width is not > MaximumPixelDimension && media.Height is not > MaximumPixelDimension;

    /// <summary>A GIF's logical screen width and height follow the six-byte signature, little-endian.</summary>
    private static NewsMediaInfo? ReadGif(ReadOnlySpan<byte> start) =>
        start.Length >= 10 && (start.StartsWith("GIF87a"u8) || start.StartsWith("GIF89a"u8)) &&
        BinaryPrimitives.ReadUInt16LittleEndian(start[6..]) is > 0 and var width &&
        BinaryPrimitives.ReadUInt16LittleEndian(start[8..]) is > 0 and var height
            ? new NewsMediaInfo("image/gif", NewsMediaKind.Picture, width, height)
            : null;

    private static NewsMediaInfo? ReadVideo(ReadOnlySpan<byte> start) =>
        IsWebm(start) ? new NewsMediaInfo("video/webm", NewsMediaKind.Video, null, null)
        : IsMp4(start) ? new NewsMediaInfo("video/mp4", NewsMediaKind.Video, null, null)
        : null;

    /// <summary>A WebM file is an EBML file whose header declares the <c>webm</c> document type.</summary>
    private static bool IsWebm(ReadOnlySpan<byte> start) =>
        start.StartsWith(EbmlSignature) && start[..Math.Min(start.Length, 64)].IndexOf(WebmDocType) >= 0;

    /// <summary>An MP4 file starts with an <c>ftyp</c> box whose major brand is an MPEG-4 brand, not QuickTime.</summary>
    private static bool IsMp4(ReadOnlySpan<byte> start) =>
        start.Length >= 12 && start[4..8].SequenceEqual("ftyp"u8) &&
        Mp4Brands.Contains(System.Text.Encoding.ASCII.GetString(start[8..12]));
}
