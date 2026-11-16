using System.Buffers.Binary;

namespace SciFiLarpNetwork.Backend.Domain.Networks;

/// <summary>
/// Accepted network background image formats. The format and pixel size are read from the file's own header,
/// never from a client-supplied name or media type, so SVG and other formats are rejected, and so is an image
/// whose declared size would need too much memory to decode.
/// </summary>
/// <remarks>
/// Only the header is read; a file with a valid header but a damaged body is still accepted, so readers must
/// treat a failed decode as "no image".
/// </remarks>
public static class NetworkImageFormats
{
    public const int MaximumByteCount = 2 * 1024 * 1024;

    /// <summary>The largest accepted width and height, in pixels.</summary>
    public const int MaximumPixelDimension = 4096;

    private static readonly byte[] PngSignature = [0x89, 0x50, 0x4E, 0x47, 0x0D, 0x0A, 0x1A, 0x0A];
    private static readonly byte[] JpegSignature = [0xFF, 0xD8];
    private static readonly byte[] RiffSignature = "RIFF"u8.ToArray();
    private static readonly byte[] WebpSignature = "WEBP"u8.ToArray();

    /// <summary>
    /// Returns the format and pixel size of a PNG, JPEG, or WebP image, or <see langword="null"/> for any other
    /// content or a header that cannot be read.
    /// </summary>
    public static NetworkImageInfo? Inspect(ReadOnlySpan<byte> content)
    {
        var info = content.StartsWith(PngSignature) ? ReadPng(content)
            : content.StartsWith(JpegSignature) ? ReadJpeg(content)
            : content.Length >= 12 && content.StartsWith(RiffSignature) && content[8..12].SequenceEqual(WebpSignature)
                ? ReadWebp(content)
                : null;
        return info is { Width: > 0, Height: > 0 } ? info : null;
    }

    /// <summary>Whether the image's width and height are both within <see cref="MaximumPixelDimension"/>.</summary>
    public static bool HasAcceptedDimensions(NetworkImageInfo image) =>
        image.Width <= MaximumPixelDimension && image.Height <= MaximumPixelDimension;

    /// <summary>The first chunk of a PNG file is IHDR, which holds the width and height as big-endian integers.</summary>
    private static NetworkImageInfo? ReadPng(ReadOnlySpan<byte> content) =>
        content.Length >= 24 && content[12..16].SequenceEqual("IHDR"u8)
            ? new NetworkImageInfo("image/png", ReadPositiveInt32(content[16..20]), ReadPositiveInt32(content[20..24]))
            : null;

    /// <summary>Walks the JPEG markers up to the first start-of-frame marker, which holds the height and width.</summary>
    private static NetworkImageInfo? ReadJpeg(ReadOnlySpan<byte> content)
    {
        var position = 2;
        while (position + 4 <= content.Length)
        {
            if (content[position] != 0xFF)
            {
                return null;
            }

            var marker = content[position + 1];
            if (marker == 0xFF)
            {
                position++;
                continue;
            }

            if (marker is 0x01 or >= 0xD0 and <= 0xD7)
            {
                position += 2;
                continue;
            }

            var length = BinaryPrimitives.ReadUInt16BigEndian(content[(position + 2)..]);
            if (length < 2)
            {
                return null;
            }

            if (IsStartOfFrame(marker))
            {
                return position + 9 <= content.Length
                    ? new NetworkImageInfo(
                        "image/jpeg",
                        BinaryPrimitives.ReadUInt16BigEndian(content[(position + 7)..]),
                        BinaryPrimitives.ReadUInt16BigEndian(content[(position + 5)..]))
                    : null;
            }

            position += 2 + length;
        }

        return null;
    }

    private static bool IsStartOfFrame(byte marker) =>
        marker is >= 0xC0 and <= 0xCF and not 0xC4 and not 0xC8 and not 0xCC;

    /// <summary>Reads the size from the first WebP chunk: extended (VP8X), lossy (VP8), or lossless (VP8L).</summary>
    private static NetworkImageInfo? ReadWebp(ReadOnlySpan<byte> content)
    {
        if (content.Length < 30)
        {
            return null;
        }

        var chunk = content[12..16];
        if (chunk.SequenceEqual("VP8X"u8))
        {
            return new NetworkImageInfo("image/webp", 1 + ReadUInt24LittleEndian(content[24..27]), 1 + ReadUInt24LittleEndian(content[27..30]));
        }

        if (chunk.SequenceEqual("VP8 "u8))
        {
            return content[23..26].SequenceEqual((ReadOnlySpan<byte>)[0x9D, 0x01, 0x2A])
                ? new NetworkImageInfo(
                    "image/webp",
                    BinaryPrimitives.ReadUInt16LittleEndian(content[26..]) & 0x3FFF,
                    BinaryPrimitives.ReadUInt16LittleEndian(content[28..]) & 0x3FFF)
                : null;
        }

        if (chunk.SequenceEqual("VP8L"u8) && content[20] == 0x2F)
        {
            var bits = BinaryPrimitives.ReadUInt32LittleEndian(content[21..]);
            return new NetworkImageInfo("image/webp", 1 + (int)(bits & 0x3FFF), 1 + (int)((bits >> 14) & 0x3FFF));
        }

        return null;
    }

    private static int ReadPositiveInt32(ReadOnlySpan<byte> bytes)
    {
        var value = BinaryPrimitives.ReadUInt32BigEndian(bytes);
        return value > int.MaxValue ? int.MaxValue : (int)value;
    }

    private static int ReadUInt24LittleEndian(ReadOnlySpan<byte> bytes) => bytes[0] | (bytes[1] << 8) | (bytes[2] << 16);
}
