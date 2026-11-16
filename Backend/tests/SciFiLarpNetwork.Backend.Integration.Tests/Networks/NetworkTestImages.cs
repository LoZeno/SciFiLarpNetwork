using System.Buffers.Binary;

namespace SciFiLarpNetwork.Backend.Tests.Integration.Networks;

/// <summary>
/// Byte arrays with a real PNG or JPEG header declaring the given pixel size, padded with varied bytes to the
/// requested length.
/// </summary>
internal static class NetworkTestImages
{
    public static byte[] Png(int size, int width = 64, int height = 64)
    {
        var header = new byte[24];
        byte[] signature = [0x89, 0x50, 0x4E, 0x47, 0x0D, 0x0A, 0x1A, 0x0A];
        signature.CopyTo(header, 0);
        BinaryPrimitives.WriteUInt32BigEndian(header.AsSpan(8), 13);
        "IHDR"u8.CopyTo(header.AsSpan(12));
        BinaryPrimitives.WriteUInt32BigEndian(header.AsSpan(16), (uint)width);
        BinaryPrimitives.WriteUInt32BigEndian(header.AsSpan(20), (uint)height);
        return Padded(header, size);
    }

    public static byte[] Jpeg(int size, int width = 64, int height = 64)
    {
        byte[] header = [0xFF, 0xD8, 0xFF, 0xC0, 0x00, 0x11, 0x08, 0, 0, 0, 0];
        BinaryPrimitives.WriteUInt16BigEndian(header.AsSpan(7), (ushort)height);
        BinaryPrimitives.WriteUInt16BigEndian(header.AsSpan(9), (ushort)width);
        return Padded(header, size);
    }

    private static byte[] Padded(byte[] header, int size)
    {
        var content = new byte[Math.Max(size, header.Length)];
        header.CopyTo(content, 0);
        for (var index = header.Length; index < content.Length; index++)
        {
            content[index] = (byte)(index * 31 % 256);
        }

        return content;
    }
}
