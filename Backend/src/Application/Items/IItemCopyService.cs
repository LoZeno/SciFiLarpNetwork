namespace SciFiLarpNetwork.Backend.Application.Items;

/// <summary>Printed copies of items. Copies are made by printing (<see cref="IItemPrintService"/>) and never deleted.</summary>
public interface IItemCopyService
{
    /// <summary>The item's copies, newest print first; <see langword="null"/> when the item does not exist.</summary>
    Task<IReadOnlyList<ItemCopySummary>?> GetCopiesAsync(Guid itemId, CancellationToken cancellationToken = default);

    /// <summary>One copy; <see langword="null"/> when no copy has this identifier.</summary>
    Task<ItemCopySummary?> GetCopyAsync(Guid copyId, CancellationToken cancellationToken = default);

    Task<ItemResult<ItemCopySummary>> DeactivateCopyAsync(Guid copyId, CancellationToken cancellationToken = default);

    Task<ItemResult<ItemCopySummary>> ReactivateCopyAsync(Guid copyId, CancellationToken cancellationToken = default);
}
