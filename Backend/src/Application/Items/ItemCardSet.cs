namespace SciFiLarpNetwork.Backend.Application.Items;

/// <summary>The cards of a print, reprint or preview, in print order.</summary>
public sealed record ItemCardSet(IReadOnlyList<ItemCard> Cards);
