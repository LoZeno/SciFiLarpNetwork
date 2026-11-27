using Marten;
using SciFiLarpNetwork.Backend.Application.Items;
using SciFiLarpNetwork.Backend.Domain.Events.ItemEvents;
using SciFiLarpNetwork.Backend.Domain.Networks;
using SciFiLarpNetwork.Backend.Infrastructure.Items.ItemDocuments;
using SciFiLarpNetwork.Backend.Infrastructure.Media;
using SciFiLarpNetwork.Backend.Infrastructure.Persistence.EventStore;

namespace SciFiLarpNetwork.Backend.Infrastructure.Items;

/// <summary>
/// Item pictures follow the network image rules (<see cref="NetworkImageFormats"/>). The file and its event cannot commit
/// together, so the file is written first and deleted again when the event does not commit. Every upload gets a new
/// identifier, and files are never deleted afterwards, so history and clones keep their references.
/// </summary>
internal sealed class ItemImageService(AttributedSessionFactory sessions, MediaFileStore files, TimeProvider timeProvider)
    : IItemImageService
{
    public async Task<ItemResult<ItemSummary>> SetImageAsync(Guid itemId, long expectedVersion, byte[] content,
        CancellationToken cancellationToken = default)
    {
        if (content.Length > NetworkImageFormats.MaximumByteCount)
        {
            return ItemResult<ItemSummary>.Failure(ItemOutcome.ImageTooLarge);
        }

        if (NetworkImageFormats.Inspect(content) is not { } image)
        {
            return ItemResult<ItemSummary>.Failure(ItemOutcome.InvalidImage);
        }

        if (!NetworkImageFormats.HasAcceptedDimensions(image))
        {
            return ItemResult<ItemSummary>.Failure(ItemOutcome.ImageDimensionsTooLarge);
        }

        await using var session = sessions.OpenSession();
        var (outcome, stream) = await session.FetchExpectedAsync<ItemDocument>(itemId, expectedVersion, cancellationToken);
        if (stream?.Aggregate is not { } current)
        {
            return ItemResult<ItemSummary>.Failure(outcome);
        }

        var imageId = Guid.NewGuid();
        session.Store(new ItemImageFileDocument(imageId, itemId, image.MediaType, content.Length));
        stream.AppendOne(new ItemImageSet(itemId, imageId, image.MediaType, content.Length, timeProvider.GetUtcNow()));
        return await CommitWithFileAsync(session, current, imageId, content, expectedVersion, cancellationToken);
    }

    /// <summary>
    /// Stores the file, then commits the session; the file is deleted again unless the session commits. Reading the
    /// item back comes after that decision, so a failed read-back never deletes the file of a committed event.
    /// </summary>
    private async Task<ItemResult<ItemSummary>> CommitWithFileAsync(IDocumentSession session, ItemDocument current,
        Guid imageId, byte[] content, long expectedVersion, CancellationToken cancellationToken)
    {
        ItemOutcome outcome;
        try
        {
            outcome = await StoreFileAsync(imageId, content, cancellationToken)
                ? await session.CommitItemAsync(sessions, current.Id, expectedVersion,
                    ItemWrites.NameOrRecipeConflict(sessions, current.Id, current.NormalizedName), cancellationToken)
                : ItemOutcome.ImageTooLarge;
        }
        catch
        {
            DeleteImageFiles(imageId);
            throw;
        }

        if (outcome != ItemOutcome.Succeeded)
        {
            DeleteImageFiles(imageId);
            return ItemResult<ItemSummary>.Failure(outcome);
        }

        return ItemResult<ItemSummary>.Success(await session.ReadBackItemAsync(current.Id));
    }

    /// <summary>
    /// Writes and commits the file. The upload was already checked against the same limit, so a refusal here only happens
    /// if the two checks drift apart; it then commits nothing rather than a cut-off file.
    /// </summary>
    private async Task<bool> StoreFileAsync(Guid imageId, byte[] content, CancellationToken cancellationToken)
    {
        using var upload = new MemoryStream(content, writable: false);
        if (await files.WriteTemporaryAsync(MediaFolder.ItemImages, imageId, upload,
                NetworkImageFormats.MaximumByteCount, cancellationToken) is null)
        {
            return false;
        }

        files.CommitTemporary(MediaFolder.ItemImages, imageId);
        return true;
    }

    private void DeleteImageFiles(Guid imageId)
    {
        files.DeleteTemporary(MediaFolder.ItemImages, imageId);
        files.Delete(MediaFolder.ItemImages, imageId);
    }

    public async Task<ItemResult<ItemSummary>> RemoveImageAsync(Guid itemId, long expectedVersion,
        CancellationToken cancellationToken = default)
    {
        await using var session = sessions.OpenSession();
        var (outcome, stream) = await session.FetchExpectedAsync<ItemDocument>(itemId, expectedVersion, cancellationToken);
        if (stream?.Aggregate is not { } current)
        {
            return ItemResult<ItemSummary>.Failure(outcome);
        }

        if (current.ImageId is null)
        {
            return ItemResult<ItemSummary>.Success(await session.SummarizeAsync(current, cancellationToken));
        }

        stream.AppendOne(new ItemImageRemoved(itemId, timeProvider.GetUtcNow()));
        return await session.SaveItemAsync(sessions, itemId, expectedVersion,
            ItemWrites.NameOrRecipeConflict(sessions, itemId, current.NormalizedName), cancellationToken);
    }

    public async Task<ItemImage?> GetImageAsync(Guid imageId, CancellationToken cancellationToken = default)
    {
        await using var query = sessions.QuerySession();
        if (await query.LoadAsync<ItemImageFileDocument>(imageId, cancellationToken) is not { } image ||
            files.Open(MediaFolder.ItemImages, imageId) is not { } file)
        {
            return null;
        }

        await using (file)
        {
            using var content = new MemoryStream(image.ByteCount);
            await file.CopyToAsync(content, cancellationToken);
            return new ItemImage(image.Id, image.MediaType, content.ToArray());
        }
    }
}
