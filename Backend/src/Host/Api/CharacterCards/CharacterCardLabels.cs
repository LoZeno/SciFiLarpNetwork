using SciFiLarpNetwork.Backend.Localization.Text;

namespace SciFiLarpNetwork.Backend.Api.CharacterCards;

/// <summary>
/// The labels a card template prints, in the request language. The names are part of the template data contract
/// documented in docs/architecture/character-cards.md; each is localized from the <c>cards.</c> key of the same name.
/// </summary>
public static class CharacterCardLabels
{
    public static readonly IReadOnlyList<string> Names =
    [
        "documentTitle", "player", "character", "gender", "species", "type", "experiencePoints", "freePoints", "skills",
        "archived"
    ];

    public static IReadOnlyDictionary<string, string> Localize(IApplicationText text) =>
        Names.ToDictionary(name => name, name => text.GetLocalizedText($"cards.{name}"), StringComparer.Ordinal);
}
