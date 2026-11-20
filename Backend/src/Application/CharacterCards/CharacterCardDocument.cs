namespace SciFiLarpNetwork.Backend.Application.CharacterCards;

/// <summary>The cards to print and the localized labels the template prints around them.</summary>
/// <param name="Language">The two-letter language code of the labels.</param>
/// <param name="Labels">Localized labels, keyed by the names documented for card templates.</param>
public sealed record CharacterCardDocument(
    string Language,
    IReadOnlyDictionary<string, string> Labels,
    IReadOnlyList<CharacterCard> Cards);
