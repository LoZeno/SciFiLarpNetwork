namespace SciFiLarpNetwork.Backend.Application.Items;

/// <summary>
/// The item catalog: definitions of game objects and their components. Items are archived, never deleted. Every
/// change to one item is checked against <c>expectedVersion</c>; a stale version reports
/// <see cref="ItemOutcome.StaleVersion"/> and changes nothing.
/// </summary>
public interface IItemService
{
    Task<ItemPage> GetItemsAsync(ItemQuery query, CancellationToken cancellationToken = default);

    Task<ItemSummary?> GetItemAsync(Guid itemId, CancellationToken cancellationToken = default);

    /// <summary>Creates an item, or a clone when <see cref="ItemCreation.ClonedFromItemId"/> is set.</summary>
    Task<ItemResult<ItemSummary>> CreateItemAsync(ItemCreation creation, CancellationToken cancellationToken = default);

    /// <summary>Replaces the item's details; its components, image and copies stay as they are.</summary>
    Task<ItemResult<ItemSummary>> UpdateItemAsync(Guid itemId, long expectedVersion, ItemDefinition definition,
        CancellationToken cancellationToken = default);

    Task<ItemResult<ItemSummary>> ArchiveItemAsync(Guid itemId, CancellationToken cancellationToken = default);

    Task<ItemResult<ItemSummary>> UnarchiveItemAsync(Guid itemId, CancellationToken cancellationToken = default);

    /// <summary>Adds a component that the item does not have yet.</summary>
    Task<ItemResult<ItemSummary>> AddComponentAsync(Guid itemId, long expectedVersion, Guid componentItemId, int quantity,
        CancellationToken cancellationToken = default);

    Task<ItemResult<ItemSummary>> ChangeComponentQuantityAsync(Guid itemId, long expectedVersion, Guid componentItemId,
        int quantity, CancellationToken cancellationToken = default);

    Task<ItemResult<ItemSummary>> RemoveComponentAsync(Guid itemId, long expectedVersion, Guid componentItemId,
        CancellationToken cancellationToken = default);
}
