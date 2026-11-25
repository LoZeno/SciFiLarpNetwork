using Marten;
using SciFiLarpNetwork.Backend.Application.Networks;
using SciFiLarpNetwork.Backend.Domain.Events.NetworkEvents;
using SciFiLarpNetwork.Backend.Domain.Networks;
using SciFiLarpNetwork.Backend.Infrastructure.Media;
using SciFiLarpNetwork.Backend.Infrastructure.Networks.NetworkDocuments;
using SciFiLarpNetwork.Backend.Infrastructure.Persistence.EventStore;

namespace SciFiLarpNetwork.Backend.Infrastructure.Networks;

/// <summary>
/// The image file and its event cannot commit together, so the file is written first and deleted again when the event
/// does not commit. Every upload gets a new identifier, and files are never deleted afterwards, so history keeps its
/// references.
/// </summary>
internal sealed class NetworkBackgroundImageService(
    AttributedSessionFactory sessions,
    MediaFileStore files,
    TimeProvider timeProvider)
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

        await using var session = sessions.OpenSession();
        var (outcome, stream) = await session.FetchExpectedNetworkAsync(networkId, expectedVersion, cancellationToken);
        if (stream?.Aggregate is null)
        {
            return NetworkResult.Failure(outcome);
        }

        var imageId = Guid.NewGuid();
        session.Store(new NetworkImageFileDocument(imageId, networkId, image.MediaType, content.Length));
        stream.AppendOne(new NetworkBackgroundImageSet(networkId, imageId, image.MediaType, content.Length,
            timeProvider.GetUtcNow()));
        var committed = await CommitWithFileAsync(session, imageId, content, cancellationToken);
        // Once committed, a cancelled read-back must not look like a failed upload.
        return committed == NetworkOutcome.Succeeded
            ? await session.LoadSavedNetworkAsync(networkId, CancellationToken.None)
            : NetworkResult.Failure(committed);
    }

    /// <summary>Stores the file, then commits the session; the file is deleted again unless the session commits.</summary>
    private async Task<NetworkOutcome> CommitWithFileAsync(IDocumentSession session, Guid imageId, byte[] content,
        CancellationToken cancellationToken)
    {
        NetworkOutcome outcome;
        try
        {
            using var upload = new MemoryStream(content, writable: false);
            await files.WriteTemporaryAsync(MediaFolder.NetworkImages, imageId, upload,
                NetworkImageFormats.MaximumByteCount, cancellationToken);
            files.CommitTemporary(MediaFolder.NetworkImages, imageId);
            outcome = await session.CommitNetworkAsync(NetworkWrites.NameConflict, cancellationToken);
        }
        catch
        {
            DeleteImageFiles(imageId);
            throw;
        }

        if (outcome != NetworkOutcome.Succeeded)
        {
            DeleteImageFiles(imageId);
        }

        return outcome;
    }

    private void DeleteImageFiles(Guid imageId)
    {
        files.DeleteTemporary(MediaFolder.NetworkImages, imageId);
        files.Delete(MediaFolder.NetworkImages, imageId);
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
        if (await query.LoadAsync<NetworkImageFileDocument>(imageId, cancellationToken) is not { } image ||
            files.Open(MediaFolder.NetworkImages, imageId) is not { } file)
        {
            return null;
        }

        await using (file)
        {
            using var content = new MemoryStream(image.ByteCount);
            await file.CopyToAsync(content, cancellationToken);
            return new NetworkImage(image.Id, image.MediaType, content.ToArray());
        }
    }
}
