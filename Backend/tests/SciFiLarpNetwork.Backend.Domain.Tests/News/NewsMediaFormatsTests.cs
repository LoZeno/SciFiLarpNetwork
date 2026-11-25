using System.Buffers.Binary;
using System.Text;
using SciFiLarpNetwork.Backend.Domain.News;

namespace SciFiLarpNetwork.Backend.Tests.Domain.News;

public sealed class NewsMediaFormatsTests
{
    public static TheoryData<string, byte[], NewsMediaInfo> ReadableMedia => new()
    {
        { "PNG", Png(640, 480), new NewsMediaInfo("image/png", NewsMediaKind.Picture, 640, 480) },
        { "JPEG", [0xFF, 0xD8, 0xFF, 0xC0, 0x00, 0x11, 0x08, 0x01, 0xE0, 0x02, 0x80], new NewsMediaInfo("image/jpeg", NewsMediaKind.Picture, 640, 480) },
        { "GIF89a", Gif("GIF89a", 320, 200), new NewsMediaInfo("image/gif", NewsMediaKind.Picture, 320, 200) },
        { "GIF87a", Gif("GIF87a", 1, 1), new NewsMediaInfo("image/gif", NewsMediaKind.Picture, 1, 1) },
        { "WebM", Webm("webm"), new NewsMediaInfo("video/webm", NewsMediaKind.Video, null, null) },
        { "MP4 isom", Mp4("isom"), new NewsMediaInfo("video/mp4", NewsMediaKind.Video, null, null) },
        { "MP4 mp42", Mp4("mp42"), new NewsMediaInfo("video/mp4", NewsMediaKind.Video, null, null) }
    };

    public static TheoryData<string, byte[]> UnreadableContent => new()
    {
        { "SVG", Encoding.UTF8.GetBytes("<svg xmlns=\"http://www.w3.org/2000/svg\"><script>alert(1)</script></svg>") },
        { "Matroska that is not WebM", Webm("matroska") },
        { "QuickTime", Mp4("qt  ") },
        { "GIF with no width", Gif("GIF89a", 0, 10) },
        { "Truncated GIF", Encoding.ASCII.GetBytes("GIF89a") },
        { "Plain text", Encoding.UTF8.GetBytes("hello") },
        { "Empty", [] }
    };

    [Theory]
    [MemberData(nameof(ReadableMedia))]
    public void TypeAndSizeAreReadFromTheHeader(string format, byte[] content, NewsMediaInfo expected)
    {
        _ = format;
        Assert.Equal(expected, NewsMediaFormats.Inspect(content));
    }

    [Theory]
    [MemberData(nameof(UnreadableContent))]
    public void OtherContentIsRefused(string format, byte[] content)
    {
        _ = format;
        Assert.Null(NewsMediaFormats.Inspect(content));
    }

    [Theory]
    [InlineData(NewsMediaKind.Picture, 5L * 1024 * 1024, true)]
    [InlineData(NewsMediaKind.Picture, (5L * 1024 * 1024) + 1, false)]
    [InlineData(NewsMediaKind.Video, 100L * 1024 * 1024, true)]
    [InlineData(NewsMediaKind.Video, (100L * 1024 * 1024) + 1, false)]
    public void SizeLimitDependsOnTheKind(NewsMediaKind kind, long byteCount, bool expected) =>
        Assert.Equal(expected, NewsMediaFormats.HasAcceptedSize(new NewsMediaInfo("x/y", kind, null, null), byteCount));

    [Theory]
    [InlineData(4096, 4096, true)]
    [InlineData(4097, 10, false)]
    [InlineData(10, 4097, false)]
    public void PictureDimensionsAreLimited(int width, int height, bool expected) =>
        Assert.Equal(expected, NewsMediaFormats.HasAcceptedDimensions(new NewsMediaInfo("image/png", NewsMediaKind.Picture, width, height)));

    [Fact]
    public void VideosHaveNoDimensionLimit() =>
        Assert.True(NewsMediaFormats.HasAcceptedDimensions(new NewsMediaInfo("video/mp4", NewsMediaKind.Video, null, null)));

    private static byte[] Png(int width, int height)
    {
        var header = new byte[24];
        byte[] signature = [0x89, 0x50, 0x4E, 0x47, 0x0D, 0x0A, 0x1A, 0x0A];
        signature.CopyTo(header, 0);
        BinaryPrimitives.WriteUInt32BigEndian(header.AsSpan(8), 13);
        "IHDR"u8.CopyTo(header.AsSpan(12));
        BinaryPrimitives.WriteUInt32BigEndian(header.AsSpan(16), (uint)width);
        BinaryPrimitives.WriteUInt32BigEndian(header.AsSpan(20), (uint)height);
        return header;
    }

    private static byte[] Gif(string signature, int width, int height)
    {
        var header = new byte[13];
        Encoding.ASCII.GetBytes(signature).CopyTo(header, 0);
        BinaryPrimitives.WriteUInt16LittleEndian(header.AsSpan(6), (ushort)width);
        BinaryPrimitives.WriteUInt16LittleEndian(header.AsSpan(8), (ushort)height);
        return header;
    }

    /// <summary>An EBML header with EBMLVersion and a DocType element, as a WebM or Matroska file starts.</summary>
    private static byte[] Webm(string docType) =>
    [
        0x1A, 0x45, 0xDF, 0xA3, 0x9F,
        0x42, 0x86, 0x81, 0x01,
        0x42, 0x82, (byte)(0x80 | docType.Length), .. Encoding.ASCII.GetBytes(docType),
        0x42, 0x87, 0x81, 0x04
    ];

    private static byte[] Mp4(string majorBrand) =>
        [0x00, 0x00, 0x00, 0x18, .. "ftyp"u8, .. Encoding.ASCII.GetBytes(majorBrand), 0x00, 0x00, 0x02, 0x00, .. "isomiso2"u8];
}
