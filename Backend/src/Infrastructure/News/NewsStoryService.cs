using JasperFx;
using JasperFx.Events;
using Marten;
using ExistingStreamIdCollisionException = Marten.Exceptions.ExistingStreamIdCollisionException;
using Marten.Pagination;
using SciFiLarpNetwork.Backend.Application.News;
using SciFiLarpNetwork.Backend.Domain.Events.NewsEvents;
using SciFiLarpNetwork.Backend.Domain.News;
using SciFiLarpNetwork.Backend.Infrastructure.Administration;
using SciFiLarpNetwork.Backend.Infrastructure.Networks.NetworkDocuments;
using SciFiLarpNetwork.Backend.Infrastructure.News.NewsDocuments;
using SciFiLarpNetwork.Backend.Infrastructure.Persistence.EventStore;
using SciFiLarpNetwork.Backend.Infrastructure.Players.PlayerDocuments;
using SciFiLarpNetwork.Common.NewsMarkdown;

namespace SciFiLarpNetwork.Backend.Infrastructure.News;

/// <summary>
/// A story is its own stream, started with the identifier the client chose, so a repeated submission collides with
/// the story already created. A save that starts using media appends to that media's stream, so it conflicts with a
/// removal decided on the media's older version (see <see cref="NewsMediaService.RemoveMediaAsync"/>).
/// </summary>
internal sealed class NewsStoryService(AttributedSessionFactory sessions, TimeProvider timeProvider) : INewsStoryService
{
    public async Task<NewsStoryPage> GetStoriesAsync(NewsStoryQuery query, CancellationToken cancellationToken = default)
    {
        if (query.NetworkId is not { } networkId)
        {
            return new NewsStoryPage([], 1, 1, 0);
        }

        await using var session = sessions.QuerySession();
        var stories = session.Query<NewsStoryDocument>().Where(story => story.NetworkId == networkId);
        if (query.ActiveAt is { } activeAt)
        {
            var time = NewsStoryTimes.ToStoredPrecision(activeAt);
            stories = stories.Where(story => story.StartsAtUtc <= time && story.EndsAtUtc > time);
        }

        var page = await stories
            .OrderByDescending(story => story.StartsAtUtc)
            .ThenByDescending(story => story.Id)
            .ToPagedListAsync(query.PageNumber, NewsStoryQuery.PageSize, cancellationToken);
        var authors = await AuthorsAsync(session, page.Select(story => story.AuthorId), cancellationToken);
        var now = timeProvider.GetUtcNow();
        return new NewsStoryPage(
            page.Select(story => new NewsStorySummary(
                    story.Id,
                    story.NetworkId,
                    Describe(authors, story.AuthorId),
                    story.Title,
                    story.StartsAtUtc,
                    story.EndsAtUtc,
                    NewsStoryTimes.StatusAt(story.StartsAtUtc, story.EndsAtUtc, now)))
                .ToArray(),
            (int)page.PageNumber,
            Math.Max(1, (int)page.PageCount),
            page.TotalItemCount);
    }

    public async Task<NewsStoryDetails?> GetStoryAsync(Guid storyId, CancellationToken cancellationToken = default)
    {
        await using var session = sessions.QuerySession();
        return await session.LoadAsync<NewsStoryDocument>(storyId, cancellationToken) is { } story
            ? await ToDetailsAsync(session, story, cancellationToken)
            : null;
    }

    public async Task<NewsResult<NewsStoryDetails>> CreateStoryAsync(Guid storyId, NewsStoryContent content,
        CancellationToken cancellationToken = default)
    {
        if (storyId == Guid.Empty)
        {
            return Failure(NewsOutcome.InvalidStoryId);
        }

        var (invalid, embeds) = Check(content);
        if (invalid is not null)
        {
            return Failure(invalid.Value);
        }

        await using var session = sessions.OpenSession();
        if (await session.LoadAsync<NewsStoryDocument>(storyId, cancellationToken) is { } existing)
        {
            return await RepeatedAsync(session, existing, content, cancellationToken);
        }

        if (await CheckReferencesAsync(session, storyId, content, embeds, current: null, cancellationToken) is { } rejected)
        {
            return Failure(rejected);
        }

        var values = StoredValues.From(content, embeds);
        session.Events.StartStream<NewsStoryDocument>(storyId, new NewsStoryCreated(
            storyId, values.NetworkId, values.AuthorId, values.Title, values.Body, values.StartsAtUtc, values.EndsAtUtc,
            values.HackingDifficulty, values.MediaIds, timeProvider.GetUtcNow()));
        try
        {
            if (!await session.TrySaveChangesAsync(cancellationToken))
            {
                return await AfterLostCreationAsync(storyId, content, NewsOutcome.ConcurrentChange, cancellationToken);
            }
        }
        catch (ExistingStreamIdCollisionException)
        {
            return await AfterLostCreationAsync(storyId, content, NewsOutcome.StoryIdInUse, cancellationToken);
        }

        return await SavedAsync(session, storyId, cancellationToken);
    }

    /// <summary>
    /// After a creation lost a race: when the same submission committed first, it is answered as a repeated submission.
    /// Two copies of a submission that place media both write to the media's stream, so the later one can fail on that
    /// stream's version before it fails on the story's identifier.
    /// </summary>
    private async Task<NewsResult<NewsStoryDetails>> AfterLostCreationAsync(
        Guid storyId,
        NewsStoryContent content,
        NewsOutcome otherwise,
        CancellationToken cancellationToken)
    {
        await using var query = sessions.QuerySession();
        return await query.LoadAsync<NewsStoryDocument>(storyId, cancellationToken) is { } committed
            ? await RepeatedAsync(query, committed, content, cancellationToken)
            : Failure(otherwise);
    }

    public async Task<NewsResult<NewsStoryDetails>> EditStoryAsync(Guid storyId, long expectedVersion,
        NewsStoryContent content, CancellationToken cancellationToken = default)
    {
        var (invalid, embeds) = Check(content);
        if (invalid is not null)
        {
            return Failure(invalid.Value);
        }

        await using var session = sessions.OpenSession();
        IEventStream<NewsStoryDocument> stream;
        try
        {
            stream = await session.Events.FetchForWriting<NewsStoryDocument>(storyId, expectedVersion, cancellationToken);
        }
        catch (ConcurrencyException)
        {
            return Failure(await session.LoadAsync<NewsStoryDocument>(storyId, cancellationToken) is null
                ? NewsOutcome.NotFound
                : NewsOutcome.ConcurrentChange);
        }

        if (stream.Aggregate is not { } current)
        {
            return Failure(NewsOutcome.NotFound);
        }

        if (await CheckReferencesAsync(session, storyId, content, embeds, current, cancellationToken) is { } rejected)
        {
            return Failure(rejected);
        }

        var values = StoredValues.From(content, embeds);
        stream.AppendOne(new NewsStoryEdited(
            storyId, values.NetworkId, values.AuthorId, values.Title, values.Body, values.StartsAtUtc, values.EndsAtUtc,
            values.HackingDifficulty, values.MediaIds, timeProvider.GetUtcNow()));
        return await session.TrySaveChangesAsync(cancellationToken)
            ? await SavedAsync(session, storyId, cancellationToken)
            : Failure(NewsOutcome.ConcurrentChange);
    }

    /// <summary>
    /// Checks the content's own rules and its body's pictures and videos, which need no stored data, parsing the body
    /// once.
    /// </summary>
    /// <returns>The first problem, or <see langword="null"/>, and the body's pictures and videos.</returns>
    private static (NewsOutcome? Problem, IReadOnlyList<NewsMediaEmbed> Embeds) Check(NewsStoryContent content)
    {
        if (NewsStoryContentValidator.Validate(content) is { } invalid)
        {
            return (invalid, []);
        }

        var scan = NewsBodyMedia.Scan(content.Body!);
        NewsOutcome? problem = scan.Problem switch
        {
            NewsBodyMediaProblem.ExternalImage => NewsOutcome.ExternalImage,
            NewsBodyMediaProblem.MissingText => NewsOutcome.MissingMediaText,
            NewsBodyMediaProblem.InvalidReference => NewsOutcome.InvalidMediaReference,
            _ => scan.Embeds.Count > NewsTexts.MaximumMediaPerStory ? NewsOutcome.TooManyMedia : null
        };
        return (problem, scan.Embeds);
    }

    /// <summary>
    /// Checks the stored data the content refers to. An edit may keep a network disabled, or an author archived, since
    /// the <paramref name="current"/> story was saved; only a new choice must be enabled and not archived.
    /// </summary>
    private async Task<NewsOutcome?> CheckReferencesAsync(
        IDocumentSession session,
        Guid storyId,
        NewsStoryContent content,
        IReadOnlyList<NewsMediaEmbed> embeds,
        NewsStoryDocument? current,
        CancellationToken cancellationToken)
    {
        if (current?.NetworkId != content.NetworkId &&
            await CheckNetworkAsync(session, content.NetworkId, cancellationToken) is { } network)
        {
            return network;
        }

        if (current?.AuthorId != content.AuthorId &&
            await CheckAuthorAsync(session, content.AuthorId, cancellationToken) is { } author)
        {
            return author;
        }

        return await ReferenceMediaAsync(session, storyId, embeds, current?.MediaIds ?? [], cancellationToken);
    }

    private static async Task<NewsOutcome?> CheckNetworkAsync(IQuerySession session, Guid networkId,
        CancellationToken cancellationToken) =>
        await session.LoadAsync<NetworkDocument>(networkId, cancellationToken) switch
        {
            null => NewsOutcome.UnknownNetwork,
            { IsEnabled: false } => NewsOutcome.NetworkDisabled,
            _ => null
        };

    private static async Task<NewsOutcome?> CheckAuthorAsync(IQuerySession session, Guid authorId,
        CancellationToken cancellationToken) =>
        await session.LoadAsync<CharacterDocument>(authorId, cancellationToken) switch
        {
            null => NewsOutcome.UnknownAuthor,
            { IsArchived: true } => NewsOutcome.AuthorArchived,
            _ => null
        };

    /// <summary>
    /// Checks that every placed media item is in the library and that only videos carry options. Media the story did
    /// not use before is fetched for writing and told it is now used, so a concurrent removal of it conflicts.
    /// </summary>
    private async Task<NewsOutcome?> ReferenceMediaAsync(
        IDocumentSession session,
        Guid storyId,
        IReadOnlyList<NewsMediaEmbed> embeds,
        IReadOnlyCollection<Guid> alreadyUsed,
        CancellationToken cancellationToken)
    {
        var media = new Dictionary<Guid, NewsMediaDocument>();
        foreach (var mediaId in embeds.Select(embed => embed.MediaId).Distinct())
        {
            NewsMediaDocument? item;
            if (alreadyUsed.Contains(mediaId))
            {
                item = await session.LoadAsync<NewsMediaDocument>(mediaId, cancellationToken);
            }
            else
            {
                var stream = await session.Events.FetchForWriting<NewsMediaDocument>(mediaId, cancellationToken);
                item = stream.Aggregate;
                if (item is { IsRemoved: false })
                {
                    stream.AppendOne(new NewsMediaReferenced(mediaId, storyId, timeProvider.GetUtcNow()));
                }
            }

            if (item is not { IsRemoved: false })
            {
                return NewsOutcome.UnknownMedia;
            }

            media[mediaId] = item;
        }

        return embeds.Any(embed => embed.Options != NewsVideoOptions.None && media[embed.MediaId].Kind == NewsMediaKind.Picture)
            ? NewsOutcome.OptionsOnPicture
            : null;
    }

    private async Task<NewsResult<NewsStoryDetails>> SavedAsync(IQuerySession session, Guid storyId,
        CancellationToken cancellationToken)
    {
        var saved = await session.LoadAsync<NewsStoryDocument>(storyId, cancellationToken);
        return Success(await ToDetailsAsync(session, saved!, cancellationToken));
    }

    private async Task<NewsResult<NewsStoryDetails>> RepeatedAsync(
        IQuerySession session,
        NewsStoryDocument existing,
        NewsStoryContent content,
        CancellationToken cancellationToken)
    {
        var values = StoredValues.From(content, []);
        return existing.NetworkId == values.NetworkId &&
               existing.AuthorId == values.AuthorId &&
               existing.Title == values.Title &&
               existing.Body == values.Body &&
               existing.StartsAtUtc == values.StartsAtUtc &&
               existing.EndsAtUtc == values.EndsAtUtc &&
               existing.HackingDifficulty == values.HackingDifficulty
            ? Success(await ToDetailsAsync(session, existing, cancellationToken))
            : Failure(NewsOutcome.StoryIdInUse);
    }

    private async Task<NewsStoryDetails> ToDetailsAsync(IQuerySession session, NewsStoryDocument story,
        CancellationToken cancellationToken)
    {
        var network = await session.LoadAsync<NetworkDocument>(story.NetworkId, cancellationToken);
        var authors = await AuthorsAsync(session, [story.AuthorId], cancellationToken);
        var media = story.MediaIds.Length == 0
            ? []
            : (await session.LoadManyAsync<NewsMediaDocument>(cancellationToken, story.MediaIds)).ToDictionary(item => item.Id);
        var administrators = await AdministratorNames.LoadAsync(
            session, [story.SavedBy, .. media.Values.Select(item => item.UploadedBy)], cancellationToken);
        return new NewsStoryDetails(
            story.Id,
            network is null
                ? new NewsNetwork(story.NetworkId, false, null, false)
                : new NewsNetwork(network.Id, true, network.Name, network.IsEnabled),
            Describe(authors, story.AuthorId),
            story.Title,
            story.Body,
            story.StartsAtUtc,
            story.EndsAtUtc,
            story.HackingDifficulty,
            NewsStoryTimes.StatusAt(story.StartsAtUtc, story.EndsAtUtc, timeProvider.GetUtcNow()),
            [.. story.MediaIds.Where(media.ContainsKey).Select(id => media[id].ToSummary(administrators))],
            story.SavedAtUtc,
            administrators.Describe(story.SavedBy),
            story.Version);
    }

    private static async Task<IReadOnlyDictionary<Guid, CharacterDocument>> AuthorsAsync(
        IQuerySession session,
        IEnumerable<Guid> authorIds,
        CancellationToken cancellationToken)
    {
        Guid[] ids = [.. authorIds.Distinct()];
        return ids.Length == 0
            ? new Dictionary<Guid, CharacterDocument>()
            : (await session.LoadManyAsync<CharacterDocument>(cancellationToken, ids)).ToDictionary(character => character.Id);
    }

    private static NewsCharacter Describe(IReadOnlyDictionary<Guid, CharacterDocument> authors, Guid authorId) =>
        authors.TryGetValue(authorId, out var author)
            ? new NewsCharacter(authorId, true, author.Number, author.Name, author.IsArchived)
            : new NewsCharacter(authorId, false, null, null, false);

    private static NewsResult<NewsStoryDetails> Success(NewsStoryDetails details) =>
        NewsResults.Success(details);

    private static NewsResult<NewsStoryDetails> Failure(NewsOutcome outcome) =>
        NewsResults.Failure<NewsStoryDetails>(outcome);

    /// <summary>The values a save stores: the title trimmed, the body as typed, and both times in UTC.</summary>
    private sealed record StoredValues(
        Guid NetworkId,
        Guid AuthorId,
        string Title,
        string Body,
        DateTimeOffset StartsAtUtc,
        DateTimeOffset EndsAtUtc,
        int HackingDifficulty,
        Guid[] MediaIds)
    {
        public static StoredValues From(NewsStoryContent content, IReadOnlyList<NewsMediaEmbed> embeds) =>
            new(content.NetworkId,
                content.AuthorId,
                content.Title!.Trim(),
                content.Body!,
                NewsStoryTimes.ToStoredPrecision(content.StartsAt),
                NewsStoryTimes.ToStoredPrecision(content.EndsAt),
                content.HackingDifficulty,
                [.. embeds.Select(embed => embed.MediaId).Distinct()]);
    }
}
