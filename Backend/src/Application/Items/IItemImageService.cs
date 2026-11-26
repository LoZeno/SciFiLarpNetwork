namespace SciFiLarpNetwork.Backend.Application.Items;

/// <summary>An item's optional picture. Stored pictures are kept after they are replaced, so history keeps its references.</summary>
public interface IItemImageService
{
    /// <summary>
    /// Stores a PNG, JPEG, or WebP image of at most 2 MB and 4096 × 4096 pixels as the item's picture, if the item is still
    /// at <paramref name="expectedVersion"/>. The format is detected from the content.
    /// </summary>
    Task<ItemResult<ItemSummary>> SetImageAsync(Guid itemId, long expectedVersion, byte[] content,
        CancellationToken cancellationToken = default);

    Task<ItemResult<ItemSummary>> RemoveImageAsync(Guid itemId, long expectedVersion,
        CancellationToken cancellationToken = default);

    Task<ItemImage?> GetImageAsync(Guid imageId, CancellationToken cancellationToken = default);
}
