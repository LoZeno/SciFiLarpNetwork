namespace SciFiLarpNetwork.Management.Api.Items;

/// <summary>An item type or a rarity; the identifier comes from <c>itemTypeId</c> or <c>rarityId</c>.</summary>
/// <param name="Position">A rarity's place in the order; 0 for item types.</param>
internal sealed record ItemCatalogEntryProperties(
    Guid? ItemTypeId,
    Guid? RarityId,
    string Name,
    int Position,
    bool IsArchived,
    long Version)
{
    public Guid Id => ItemTypeId ?? RarityId ?? Guid.Empty;
}
