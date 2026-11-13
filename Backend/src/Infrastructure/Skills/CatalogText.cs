namespace SciFiLarpNetwork.Backend.Infrastructure.Skills;

internal static class CatalogText
{
    public static string? NormalizeDescription(string? description) =>
        string.IsNullOrWhiteSpace(description) ? null : description.Trim();
}
