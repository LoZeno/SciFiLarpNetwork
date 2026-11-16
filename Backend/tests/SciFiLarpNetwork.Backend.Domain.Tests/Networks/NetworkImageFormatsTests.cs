using System.Text;
using SciFiLarpNetwork.Backend.Domain.Networks;

namespace SciFiLarpNetwork.Backend.Tests.Domain.Networks;

public sealed class NetworkImageFormatsTests
{
    private static readonly byte[] PngSignature = [0x89, 0x50, 0x4E, 0x47, 0x0D, 0x0A, 0x1A, 0x0A];

    public static TheoryData<string, byte[], NetworkImageInfo> ReadableImages => new()
    {
        { "PNG", Png(width: 640, height: 480), new NetworkImageInfo("image/png", 640, 480) },
        { "JPEG baseline", [0xFF, 0xD8, .. Sof(0xC0, width: 800, height: 600)], new NetworkImageInfo("image/jpeg", 800, 600) },
        {
            "JPEG with APP0, fill byte and progressive frame",
            [0xFF, 0xD8, 0xFF, 0xE0, 0x00, 0x04, 0x4A, 0x46, 0xFF, .. Sof(0xC2, width: 1024, height: 768)],
            new NetworkImageInfo("image/jpeg", 1024, 768)
        },
        {
            "JPEG with a restart marker before the frame",
            [0xFF, 0xD8, 0xFF, 0xD0, .. Sof(0xC1, width: 12, height: 34)],
            new NetworkImageInfo("image/jpeg", 12, 34)
        },
        { "WebP extended", Webp("VP8X", [0x00, 0x00, 0x00, 0x00, 0x7F, 0x02, 0x00, 0xDF, 0x01, 0x00]), new NetworkImageInfo("image/webp", 640, 480) },
        { "WebP lossy", Webp("VP8 ", [0x00, 0x00, 0x00, 0x9D, 0x01, 0x2A, 0x80, 0x02, 0xE0, 0x01]), new NetworkImageInfo("image/webp", 640, 480) },
        { "WebP lossless", Webp("VP8L", [0x2F, .. LosslessSize(width: 640, height: 480), 0x00, 0x00, 0x00, 0x00, 0x00]), new NetworkImageInfo("image/webp", 640, 480) }
    };

    public static TheoryData<string, byte[]> UnreadableContent => new()
    {
        { "SVG", Encoding.UTF8.GetBytes("<svg xmlns=\"http://www.w3.org/2000/svg\"><script>alert(1)</script></svg>") },
        { "GIF", Encoding.ASCII.GetBytes("GIF89a") },
        { "RIFF audio", [.. "RIFF"u8, 0x10, 0x00, 0x00, 0x00, .. "WAVE"u8] },
        { "short RIFF", [.. "RIFF"u8, 0x10] },
        { "empty", [] },
        { "PNG signature only", PngSignature },
        { "PNG without IHDR", [.. PngSignature, 0x00, 0x00, 0x00, 0x0D, .. "IDAT"u8, 0, 0, 0, 1, 0, 0, 0, 1] },
        { "PNG with zero width", Png(width: 0, height: 10) },
        { "JPEG without a frame", [0xFF, 0xD8, 0xFF, 0xE0, 0x00, 0x04, 0x4A, 0x46] },
        { "JPEG with garbage after the signature", [0xFF, 0xD8, 0x12, 0x34, 0x56, 0x78] },
        { "JPEG with a zero segment length", [0xFF, 0xD8, 0xFF, 0xE0, 0x00, 0x00, 0x00, 0x00] },
        { "JPEG with a truncated frame", [0xFF, 0xD8, 0xFF, 0xC0, 0x00, 0x11, 0x08] },
        { "WebP lossy without its start code", Webp("VP8 ", [0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x80, 0x02, 0xE0, 0x01]) },
        { "WebP lossless without its signature", Webp("VP8L", [0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00]) },
        { "WebP with an unknown chunk", Webp("ALPH", [0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00]) },
        { "truncated WebP", [.. "RIFF"u8, 0x10, 0x00, 0x00, 0x00, .. "WEBP"u8, .. "VP8X"u8] }
    };

    [Theory]
    [MemberData(nameof(ReadableImages))]
    public void HeadersGiveTheFormatAndPixelSize(string name, byte[] content, NetworkImageInfo expected)
    {
        _ = name;

        Assert.Equal(expected, NetworkImageFormats.Inspect(content));
    }

    [Theory]
    [MemberData(nameof(UnreadableContent))]
    public void OtherOrUnreadableContentIsRejected(string name, byte[] content)
    {
        _ = name;

        Assert.Null(NetworkImageFormats.Inspect(content));
    }

    [Fact]
    public void AHugeDeclaredPngWidthIsCappedRatherThanNegative()
    {
        Assert.Equal(int.MaxValue, NetworkImageFormats.Inspect(Png(width: -1, height: 10))!.Width);
    }

    [Theory]
    [InlineData(NetworkImageFormats.MaximumPixelDimension, NetworkImageFormats.MaximumPixelDimension, true)]
    [InlineData(NetworkImageFormats.MaximumPixelDimension + 1, 1, false)]
    [InlineData(1, NetworkImageFormats.MaximumPixelDimension + 1, false)]
    public void DimensionsAreAcceptedUpToTheLimit(int width, int height, bool expected)
    {
        Assert.Equal(expected, NetworkImageFormats.HasAcceptedDimensions(new NetworkImageInfo("image/png", width, height)));
    }

    private static byte[] Png(int width, int height) =>
        [.. PngSignature, 0x00, 0x00, 0x00, 0x0D, .. "IHDR"u8, .. BigEndian(width), .. BigEndian(height), 0x08, 0x06, 0x00, 0x00, 0x00];

    private static byte[] Sof(byte marker, int width, int height) =>
        [0xFF, marker, 0x00, 0x11, 0x08, (byte)(height >> 8), (byte)height, (byte)(width >> 8), (byte)width, 0x03];

    private static byte[] Webp(string chunk, byte[] data) =>
        [.. "RIFF"u8, 0x10, 0x00, 0x00, 0x00, .. "WEBP"u8, .. Encoding.ASCII.GetBytes(chunk), 0x0A, 0x00, 0x00, 0x00, .. data];

    private static byte[] LosslessSize(int width, int height)
    {
        var bits = (uint)(width - 1) | ((uint)(height - 1) << 14);
        return [(byte)bits, (byte)(bits >> 8), (byte)(bits >> 16), (byte)(bits >> 24)];
    }

    private static byte[] BigEndian(int value) =>
        [(byte)(value >> 24), (byte)(value >> 16), (byte)(value >> 8), (byte)value];
}
