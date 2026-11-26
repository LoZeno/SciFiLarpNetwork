using SciFiLarpNetwork.Backend.Localization.Text;

namespace SciFiLarpNetwork.Backend.Api.Items;

/// <summary>
/// The labels an item-card template prints, in the request language. The names are part of the template data contract
/// documented in docs/architecture/item-cards.md; each is localized from the <c>itemCards.</c> key of the same name.
/// </summary>
public static class ItemCardLabels
{
    public static readonly IReadOnlyList<string> Names = ["documentTitle", "description", "effect", "charges", "expires", "archived"];

    public static IReadOnlyDictionary<string, string> Localize(IApplicationText text) =>
        Names.ToDictionary(name => name, name => text.GetLocalizedText($"itemCards.{name}"), StringComparer.Ordinal);
}
