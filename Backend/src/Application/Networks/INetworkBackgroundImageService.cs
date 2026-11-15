namespace SciFiLarpNetwork.Backend.Application.Networks;

/// <summary>
/// Network background images. An uploaded image replaces the theme's background image; removing it restores
/// the theme's image. Stored images are kept, so history keeps its references.
/// </summary>
public interface INetworkBackgroundImageService
{
    /// <summary>
    /// Stores a PNG, JPEG, or WebP image of at most 2 MB as the network's background, if the network is still at
    /// <paramref name="expectedVersion"/>. The format is detected from the content.
    /// </summary>
    Task<NetworkResult> SetBackgroundImageAsync(
        Guid networkId,
        long expectedVersion,
        byte[] content,
        CancellationToken cancellationToken = default);

    Task<NetworkResult> RemoveBackgroundImageAsync(Guid networkId, long expectedVersion,
        CancellationToken cancellationToken = default);

    Task<NetworkImage?> GetImageAsync(Guid imageId, CancellationToken cancellationToken = default);
}
