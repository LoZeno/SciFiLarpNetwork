namespace SciFiLarpNetwork.Backend.Application.Items;

/// <summary>The item cards to print and the localized labels the template prints around them.</summary>
/// <param name="Language">The two-letter language code of the labels.</param>
/// <param name="Labels">Localized labels, keyed by the names documented for item-card templates.</param>
public sealed record ItemCardDocument(
    string Language,
    IReadOnlyDictionary<string, string> Labels,
    IReadOnlyList<ItemCard> Cards);
