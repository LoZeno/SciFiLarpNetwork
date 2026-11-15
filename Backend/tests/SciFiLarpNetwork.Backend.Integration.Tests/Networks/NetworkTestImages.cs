namespace SciFiLarpNetwork.Backend.Tests.Integration.Networks;

/// <summary>Byte arrays that start with a real image signature, padded with varied bytes to the requested size.</summary>
internal static class NetworkTestImages
{
    public static byte[] Png(int size) => Padded([0x89, 0x50, 0x4E, 0x47, 0x0D, 0x0A, 0x1A, 0x0A], size);

    public static byte[] Jpeg(int size) => Padded([0xFF, 0xD8, 0xFF, 0xE0], size);

    private static byte[] Padded(byte[] signature, int size)
    {
        var content = new byte[size];
        signature.CopyTo(content, 0);
        for (var index = signature.Length; index < size; index++)
        {
            content[index] = (byte)(index * 31 % 256);
        }

        return content;
    }
}
