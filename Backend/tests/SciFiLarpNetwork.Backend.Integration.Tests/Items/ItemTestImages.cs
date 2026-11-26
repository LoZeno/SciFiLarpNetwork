using SciFiLarpNetwork.Backend.Tests.Integration.Networks;

namespace SciFiLarpNetwork.Backend.Tests.Integration.Items;

internal static class ItemTestImages
{
    public static byte[] Png(int size = 1024) => NetworkTestImages.Png(size);
}
