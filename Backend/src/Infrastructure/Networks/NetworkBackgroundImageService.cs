using Marten;
using SciFiLarpNetwork.Backend.Application.Networks;
using SciFiLarpNetwork.Backend.Domain.Events.NetworkEvents;
using SciFiLarpNetwork.Backend.Domain.Networks;
using SciFiLarpNetwork.Backend.Infrastructure.Networks.NetworkDocuments;
using SciFiLarpNetwork.Backend.Infrastructure.Persistence.EventStore;

namespace SciFiLarpNetwork.Backend.Infrastructure.Networks;

/// <summary>
/// The image document and the event that references it are written in one session, so they commit together.
/// </summary>
internal sealed class NetworkBackgroundImageService(AttributedSessionFactory sessions, TimeProvider timeProvider)
    : INetworkBackgroundImageService
{
    public async Task<NetworkResult> SetBackgroundImageAsync(
        Guid networkId,
        long expectedVersion,
        byte[] content,
        CancellationToken cancellationToken = default)
    {
        if (content.Length > NetworkImageFormats.MaximumByteCount)
        {
            return NetworkResult.Failure(NetworkOutcome.ImageTooLarge);
        }

        if (NetworkImageFormats.Inspect(content) is not { } image)
        {
            return NetworkResult.Failure(NetworkOutcome.InvalidImage);
        }

        if (!NetworkImageFormats.HasAcceptedDimensions(image))
        {
            return NetworkResult.Failure(NetworkOutcome.ImageDimensionsTooLarge);
        }

        var mediaType = image.MediaType;

        await using var session = sessions.OpenSession();
        var (outcome, stream) = await session.FetchExpectedNetworkAsync(networkId, expectedVersion, cancellationToken);
        if (stream?.Aggregate is null)
        {
            return NetworkResult.Failure(outcome);
        }

        var imageId = Guid.NewGuid();
        session.Store(new NetworkImageDocument(imageId, networkId, mediaType, content));
        stream.AppendOne(new NetworkBackgroundImageSet(networkId, imageId, mediaType, content.Length,
            timeProvider.GetUtcNow()));
        return await session.SaveNetworkAsync(networkId, NetworkWrites.NameConflict, cancellationToken);
    }

    public async Task<NetworkResult> RemoveBackgroundImageAsync(Guid networkId, long expectedVersion,
        CancellationToken cancellationToken = default)
    {
        await using var session = sessions.OpenSession();
        var (outcome, stream) = await session.FetchExpectedNetworkAsync(networkId, expectedVersion, cancellationToken);
        if (stream?.Aggregate is not { } current)
        {
            return NetworkResult.Failure(outcome);
        }

        if (current.BackgroundImageId is null)
        {
            return NetworkResult.Success(current);
        }

        stream.AppendOne(new NetworkBackgroundImageRemoved(networkId, timeProvider.GetUtcNow()));
        return await session.SaveNetworkAsync(networkId, NetworkWrites.NameConflict, cancellationToken);
    }

    public async Task<NetworkImage?> GetImageAsync(Guid imageId, CancellationToken cancellationToken = default)
    {
        await using var query = sessions.QuerySession();
        return await query.LoadAsync<NetworkImageDocument>(imageId, cancellationToken) is { } image
            ? new NetworkImage(image.Id, image.MediaType, image.Content)
            : null;
    }
}
