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
        try
        {
            var stored = await StoreAsync(mediaId, name!.Trim(), content, cancellationToken);
            if (stored.Outcome != NewsOutcome.Succeeded)
            {
                files.DeleteTemporary(mediaId);
            }

            return stored;
        }
        catch
        {
            files.DeleteTemporary(mediaId);
            files.Delete(mediaId);
            throw;
        }
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

    private async Task<NewsResult<NewsMediaSummary>> StoreAsync(Guid mediaId, string name, Stream content,
        CancellationToken cancellationToken)
    {
        if (await files.WriteTemporaryAsync(mediaId, content, NewsMediaFormats.MaximumByteCount, cancellationToken)
            is not { } byteCount)
        {
            return NewsResults.Failure<NewsMediaSummary>(NewsOutcome.MediaTooLarge);
        }

        var start = await files.ReadTemporaryStartAsync(mediaId, NewsMediaFormats.MaximumPictureByteCount, cancellationToken);
        if (NewsMediaFormats.Inspect(start) is not { } media)
        {
            return NewsResults.Failure<NewsMediaSummary>(NewsOutcome.InvalidMediaFile);
        }

        if (!NewsMediaFormats.HasAcceptedSize(media, byteCount))
        {
            return NewsResults.Failure<NewsMediaSummary>(NewsOutcome.MediaTooLarge);
        }

        if (!NewsMediaFormats.HasAcceptedDimensions(media))
        {
            return NewsResults.Failure<NewsMediaSummary>(NewsOutcome.MediaDimensionsTooLarge);
        }

        files.CommitTemporary(mediaId);
        await using var session = sessions.OpenSession();
        session.Events.StartStream<NewsMediaDocument>(mediaId, new NewsMediaUploaded(
            mediaId, name, media.MediaType, media.Kind, byteCount, media.Width, media.Height, timeProvider.GetUtcNow()));
        await session.SaveChangesAsync(cancellationToken);
        var saved = await session.LoadAsync<NewsMediaDocument>(mediaId, cancellationToken);
        var administrators = await NewsAdministrators.LoadAsync(session, [saved!.UploadedBy], cancellationToken);
        return NewsResults.Success(saved.ToSummary(administrators));
    }
}
