using Marten;
using SciFiLarpNetwork.Backend.Application.News;
using SciFiLarpNetwork.Backend.Domain.Events.NewsEvents;
using SciFiLarpNetwork.Backend.Domain.News;
using SciFiLarpNetwork.Backend.Infrastructure.News.NewsDocuments;
using SciFiLarpNetwork.Backend.Infrastructure.Persistence.EventStore;

namespace SciFiLarpNetwork.Backend.Infrastructure.News;

/// <summary>
/// The file and its upload event cannot commit together, so the file is written first and deleted again when the event
/// does not commit; a removal commits its event first and then deletes the file.
/// </summary>
internal sealed class NewsMediaService(AttributedSessionFactory sessions, NewsMediaFiles files, TimeProvider timeProvider)
    : INewsMediaService
{
    public async Task<IReadOnlyList<NewsMediaSummary>> GetMediaAsync(CancellationToken cancellationToken = default)
    {
        await using var session = sessions.QuerySession();
        var media = await session.Query<NewsMediaDocument>()
            .Where(item => !item.IsRemoved)
            .OrderByDescending(item => item.UploadedAtUtc)
            .ThenByDescending(item => item.Id)
            .ToListAsync(cancellationToken);
        var administrators = await NewsAdministrators.LoadAsync(session, media.Select(item => item.UploadedBy), cancellationToken);
        return [.. media.Select(item => item.ToSummary(administrators))];
    }

    public async Task<NewsResult<NewsMediaSummary>> UploadMediaAsync(string? name, Stream content,
        CancellationToken cancellationToken = default)
    {
        if (!NewsTexts.IsValidMediaName(name))
        {
            return NewsResults.Failure<NewsMediaSummary>(NewsOutcome.InvalidMediaName);
        }

        var mediaId = Guid.NewGuid();
        NewsOutcome outcome;
        try
        {
            outcome = await StoreAsync(mediaId, name!.Trim(), content, cancellationToken);
        }
        catch
        {
            files.DeleteTemporary(mediaId);
            files.Delete(mediaId);
            throw;
        }

        if (outcome != NewsOutcome.Succeeded)
        {
            files.DeleteTemporary(mediaId);
            return NewsResults.Failure<NewsMediaSummary>(outcome);
        }

        return NewsResults.Success(await CommittedSummaryAsync(mediaId));
    }

    /// <summary>
    /// Reads back media whose upload has committed. It ignores the request's cancellation: the upload is done, and a
    /// cancelled read must not look like a failed upload, whose file would then be deleted.
    /// </summary>
    private async Task<NewsMediaSummary> CommittedSummaryAsync(Guid mediaId)
    {
        await using var session = sessions.QuerySession();
        var saved = await session.LoadAsync<NewsMediaDocument>(mediaId, CancellationToken.None);
        var administrators = await NewsAdministrators.LoadAsync(session, [saved!.UploadedBy], CancellationToken.None);
        return saved.ToSummary(administrators);
    }

    public async Task<NewsOutcome> RemoveMediaAsync(Guid mediaId, CancellationToken cancellationToken = default)
    {
        await using var session = sessions.OpenSession();
        var stream = await session.Events.FetchForWriting<NewsMediaDocument>(mediaId, cancellationToken);
        if (stream.Aggregate is not { IsRemoved: false })
        {
            return NewsOutcome.NotFound;
        }

        if (await session.Query<NewsStoryDocument>().AnyAsync(story => story.MediaIds.Contains(mediaId), cancellationToken))
        {
            return NewsOutcome.MediaInUse;
        }

        stream.AppendOne(new NewsMediaRemoved(mediaId, timeProvider.GetUtcNow()));
        if (!await session.TrySaveChangesAsync(cancellationToken))
        {
            return NewsOutcome.ConcurrentChange;
        }

        files.Delete(mediaId);
        return NewsOutcome.Succeeded;
    }

    public async Task<NewsMediaContent?> OpenMediaAsync(Guid mediaId, CancellationToken cancellationToken = default)
    {
        await using var session = sessions.QuerySession();
        return await session.LoadAsync<NewsMediaDocument>(mediaId, cancellationToken) is { IsRemoved: false } media &&
               files.Open(mediaId) is { } content
            ? new NewsMediaContent(media.MediaType, content)
            : null;
    }

    /// <summary>Checks the upload, gives the file its final name, and commits the upload event.</summary>
    private async Task<NewsOutcome> StoreAsync(Guid mediaId, string name, Stream content,
        CancellationToken cancellationToken)
    {
        if (await files.WriteTemporaryAsync(mediaId, content, NewsMediaFormats.MaximumByteCount, cancellationToken)
            is not { } byteCount)
        {
            return NewsOutcome.MediaTooLarge;
        }

        var start = await files.ReadTemporaryStartAsync(mediaId, NewsMediaFormats.MaximumPictureByteCount, cancellationToken);
        if (NewsMediaFormats.Inspect(start) is not { } media)
        {
            return NewsOutcome.InvalidMediaFile;
        }

        if (!NewsMediaFormats.HasAcceptedSize(media, byteCount))
        {
            return NewsOutcome.MediaTooLarge;
        }

        if (!NewsMediaFormats.HasAcceptedDimensions(media))
        {
            return NewsOutcome.MediaDimensionsTooLarge;
        }

        files.CommitTemporary(mediaId);
        await using var session = sessions.OpenSession();
        session.Events.StartStream<NewsMediaDocument>(mediaId, new NewsMediaUploaded(
            mediaId, name, media.MediaType, media.Kind, byteCount, media.Width, media.Height, timeProvider.GetUtcNow()));
        await session.SaveChangesAsync(cancellationToken);
        return NewsOutcome.Succeeded;
    }
}
