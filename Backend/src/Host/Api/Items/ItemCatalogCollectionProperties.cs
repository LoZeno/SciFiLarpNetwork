namespace SciFiLarpNetwork.Backend.Api.Items;

/// <summary>The item type or rarity list.</summary>
public sealed record ItemCatalogCollectionProperties(string Title, string Description, bool IncludesArchived, int MaximumNameLength);
