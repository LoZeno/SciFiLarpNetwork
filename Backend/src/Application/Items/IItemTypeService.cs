namespace SciFiLarpNetwork.Backend.Application.Items;

/// <summary>The item type catalog. Types are listed by name and are archived, never deleted.</summary>
public interface IItemTypeService
{
    Task<IReadOnlyList<ItemTypeSummary>> GetItemTypesAsync(bool includeArchived, CancellationToken cancellationToken = default);

    Task<ItemTypeSummary?> GetItemTypeAsync(Guid itemTypeId, CancellationToken cancellationToken = default);

    Task<ItemResult<ItemTypeSummary>> CreateItemTypeAsync(string name, CancellationToken cancellationToken = default);

    /// <summary>
    /// Renames the type if it is still at <paramref name="expectedVersion"/>; otherwise reports
    /// <see cref="ItemOutcome.StaleVersion"/> and changes nothing.
    /// </summary>
    Task<ItemResult<ItemTypeSummary>> RenameItemTypeAsync(Guid itemTypeId, long expectedVersion, string name,
        CancellationToken cancellationToken = default);

    Task<ItemResult<ItemTypeSummary>> ArchiveItemTypeAsync(Guid itemTypeId, CancellationToken cancellationToken = default);

    Task<ItemResult<ItemTypeSummary>> UnarchiveItemTypeAsync(Guid itemTypeId, CancellationToken cancellationToken = default);
}
