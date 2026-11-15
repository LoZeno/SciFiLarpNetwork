using System.Text;
using SciFiLarpNetwork.Backend.Domain.Networks;

namespace SciFiLarpNetwork.Backend.Tests.Domain.Networks;

public sealed class NetworkImageFormatsTests
{
    public static TheoryData<byte[], string> AcceptedImages => new()
    {
        { [0x89, 0x50, 0x4E, 0x47, 0x0D, 0x0A, 0x1A, 0x0A, 0x00], "image/png" },
        { [0xFF, 0xD8, 0xFF, 0xE0, 0x00], "image/jpeg" },
        { [.. "RIFF"u8, 0x10, 0x00, 0x00, 0x00, .. "WEBPVP8 "u8], "image/webp" }
    };

    public static TheoryData<byte[]> RejectedContent => new()
    {
        { Encoding.UTF8.GetBytes("<svg xmlns=\"http://www.w3.org/2000/svg\"><script>alert(1)</script></svg>") },
        { Encoding.ASCII.GetBytes("GIF89a") },
        { [.. "RIFF"u8, 0x10, 0x00, 0x00, 0x00, .. "WAVE"u8] },
        { [.. "RIFF"u8, 0x10] },
        { [0x89, 0x50, 0x4E] },
        { [] }
    };

    [Theory]
    [MemberData(nameof(AcceptedImages))]
    public void AcceptedFormatsAreRecognizedFromTheirLeadingBytes(byte[] content, string expectedMediaType)
    {
        Assert.Equal(expectedMediaType, NetworkImageFormats.DetectMediaType(content));
    }

    [Theory]
    [MemberData(nameof(RejectedContent))]
    public void OtherContentIsRejected(byte[] content)
    {
        Assert.Null(NetworkImageFormats.DetectMediaType(content));
    }
}
