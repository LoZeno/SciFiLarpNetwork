namespace SciFiLarpNetwork.Backend.Infrastructure.Items;

/// <summary>A copy's printed QR code is its identifier written as 32 lower-case hexadecimal digits.</summary>
internal static class ItemCopyCodes
{
    public static string Format(Guid copyId) => copyId.ToString("N");
}
