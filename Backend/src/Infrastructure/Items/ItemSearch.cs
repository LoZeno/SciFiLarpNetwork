using SciFiLarpNetwork.Backend.Domain.Skills;

namespace SciFiLarpNetwork.Backend.Infrastructure.Items;

/// <summary>
/// The item list searches one stored text holding the name, description and effect, normalized like names, so the
/// search ignores case and compatibility forms.
/// </summary>
internal static class ItemSearch
{
    public static string Text(string name, string? description, string? effect) =>
        CatalogNames.Normalize(string.Join('\n', name, description ?? string.Empty, effect ?? string.Empty));

    /// <summary>The normalized search text, or <see langword="null"/> when blank (matches every item).</summary>
    public static string? Normalize(string? search) =>
        string.IsNullOrWhiteSpace(search) ? null : CatalogNames.Normalize(search);
}
