using SciFiLarpNetwork.Backend.Application.News;
using SciFiLarpNetwork.Backend.Domain.Characters;
using SciFiLarpNetwork.Backend.Domain.News;
using SciFiLarpNetwork.Backend.Tests.Integration.Api;

namespace SciFiLarpNetwork.Backend.Tests.Integration.News;

[Collection("Backend database integration")]
public sealed class NewsStoryServiceTests(AdministratorAuthenticationFactory factory) : IAsyncLifetime
{
    private readonly NewsTestData _data = new(factory);

    private INewsStoryService Stories => _data.Stories;

    private static CancellationToken Token => TestContext.Current.CancellationToken;

    public async ValueTask InitializeAsync() => await factory.ResetAsync();

    public ValueTask DisposeAsync() => ValueTask.CompletedTask;

    [Fact]
    public async Task CreatedStoryHasItsValues()
    {
        var network = await _data.CreateNetworkAsync();
        var author = await _data.CreateCharacterAsync("Kira");
        var content = _data.Content(network, author) with { Title = "  Fleet arrives ", Body = "  **Tonight**\r\n\r\n— K.  " };
        var storyId = Guid.NewGuid();

        var created = (await Stories.CreateStoryAsync(storyId, content, Token)).Value!;

        Assert.Equivalent(
            new NewsStoryDetails(
                storyId,
                new NewsNetwork(network.NetworkId, true, "Core network", true),
                new NewsCharacter(author.CharacterId, true, author.Number, "Kira", false),
                "Fleet arrives",
                "  **Tonight**\r\n\r\n— K.  ",
                NewsStoryTimes.ToStoredPrecision(content.StartsAt),
                NewsStoryTimes.ToStoredPrecision(content.EndsAt),
                2,
                NewsStoryStatus.Active,
                [],
                created.SavedAtUtc,
                null,
                1),
            created,
            strict: true);
    }

    [Fact]
    public async Task TimesAreStoredInUtc()
    {
        var network = await _data.CreateNetworkAsync();
        var author = await _data.CreateCharacterAsync();
        var startsAt = new DateTimeOffset(2026, 12, 5, 20, 30, 0, TimeSpan.FromHours(2));
        var content = _data.Content(network, author) with { StartsAt = startsAt, EndsAt = startsAt.AddHours(3) };

        var created = await _data.CreateAsync(content);

        Assert.Equal(
            new { Start = new DateTimeOffset(2026, 12, 5, 18, 30, 0, TimeSpan.Zero), Offset = TimeSpan.Zero },
            new { Start = created.StartsAtUtc, Offset = created.StartsAtUtc.Offset });
    }

    [Fact]
    public async Task InvalidContentIsRejectedAndNotStored()
    {
        var network = await _data.CreateNetworkAsync();
        var author = await _data.CreateCharacterAsync();
        var content = _data.Content(network, author);

        var result = await Stories.CreateStoryAsync(Guid.NewGuid(), content with { EndsAt = content.StartsAt }, Token);

        var stored = await Stories.GetStoriesAsync(new NewsStoryQuery(network.NetworkId, null, 1), Token);
        Assert.Equal(new { Outcome = NewsOutcome.InvalidTimes, Stored = 0L }, new { result.Outcome, Stored = stored.TotalCount });
    }

    [Fact]
    public async Task EmptyStoryIdIsRejected()
    {
        var network = await _data.CreateNetworkAsync();
        var author = await _data.CreateCharacterAsync();

        var result = await Stories.CreateStoryAsync(Guid.Empty, _data.Content(network, author), Token);

        Assert.Equal(NewsOutcome.InvalidStoryId, result.Outcome);
    }

    [Fact]
    public async Task UnknownNetworkIsRejected()
    {
        var network = await _data.CreateNetworkAsync();
        var author = await _data.CreateCharacterAsync();

        var result = await Stories.CreateStoryAsync(Guid.NewGuid(), _data.Content(network, author) with { NetworkId = Guid.NewGuid() }, Token);

        Assert.Equal(NewsOutcome.UnknownNetwork, result.Outcome);
    }

    [Fact]
    public async Task DisabledNetworkIsRejected()
    {
        await _data.CreateNetworkAsync();
        var disabled = await _data.CreateDisabledNetworkAsync();
        var author = await _data.CreateCharacterAsync();

        var result = await Stories.CreateStoryAsync(Guid.NewGuid(), _data.Content(disabled, author), Token);

        Assert.Equal(NewsOutcome.NetworkDisabled, result.Outcome);
    }

    [Fact]
    public async Task UnknownAuthorIsRejected()
    {
        var network = await _data.CreateNetworkAsync();
        var author = await _data.CreateCharacterAsync();

        var result = await Stories.CreateStoryAsync(Guid.NewGuid(), _data.Content(network, author) with { AuthorId = Guid.NewGuid() }, Token);

        Assert.Equal(NewsOutcome.UnknownAuthor, result.Outcome);
    }

    [Fact]
    public async Task ArchivedAuthorIsRejected()
    {
        var network = await _data.CreateNetworkAsync();
        var author = await _data.CreateArchivedCharacterAsync();

        var result = await Stories.CreateStoryAsync(Guid.NewGuid(), _data.Content(network, author), Token);

        Assert.Equal(NewsOutcome.AuthorArchived, result.Outcome);
    }

    [Theory]
    [InlineData(CharacterStatus.Retired)]
    [InlineData(CharacterStatus.Dead)]
    public async Task AuthorOfAnyStatusCanWrite(CharacterStatus status)
    {
        var network = await _data.CreateNetworkAsync();
        var author = await _data.CreateCharacterAsync("Kira", status);

        var result = await Stories.CreateStoryAsync(Guid.NewGuid(), _data.Content(network, author), Token);

        Assert.Equal(NewsOutcome.Succeeded, result.Outcome);
    }

    [Fact]
    public async Task RepeatedSubmissionReturnsTheSameStoryAndStoresOne()
    {
        var network = await _data.CreateNetworkAsync();
        var author = await _data.CreateCharacterAsync();
        var content = _data.Content(network, author);
        var storyId = Guid.NewGuid();
        await Stories.CreateStoryAsync(storyId, content, Token);

        var repeated = await Stories.CreateStoryAsync(storyId, content, Token);

        var stored = await Stories.GetStoriesAsync(new NewsStoryQuery(network.NetworkId, null, 1), Token);
        Assert.Equal(
            new { Outcome = NewsOutcome.Succeeded, StoryId = (Guid?)storyId, Stored = 1L },
            new { repeated.Outcome, StoryId = repeated.Value?.StoryId, Stored = stored.TotalCount });
    }

    [Fact]
    public async Task SameIdentifierWithOtherContentIsRejected()
    {
        var network = await _data.CreateNetworkAsync();
        var author = await _data.CreateCharacterAsync();
        var content = _data.Content(network, author);
        var storyId = Guid.NewGuid();
        await Stories.CreateStoryAsync(storyId, content, Token);

        var result = await Stories.CreateStoryAsync(storyId, content with { Title = "Other" }, Token);

        Assert.Equal(NewsOutcome.StoryIdInUse, result.Outcome);
    }

    [Fact]
    public async Task EditReplacesEveryValue()
    {
        var network = await _data.CreateNetworkAsync();
        var other = await _data.CreateNetworkAsync("outer", "Outer rim");
        var author = await _data.CreateCharacterAsync("Kira");
        var newAuthor = await _data.CreateCharacterAsync("Jon");
        var story = await _data.CreateAsync(_data.Content(network, author));
        var startsAt = factory.Clock.GetUtcNow().AddDays(1);
        var edit = new NewsStoryContent(other.NetworkId, newAuthor.CharacterId, "Fleet delayed", "Not tonight.", startsAt, startsAt.AddHours(2), 3);

        var edited = (await Stories.EditStoryAsync(story.StoryId, story.Version, edit, Token)).Value!;

        Assert.Equal(
            new { Network = other.NetworkId, Author = newAuthor.CharacterId, Title = "Fleet delayed", Body = "Not tonight.", Start = NewsStoryTimes.ToStoredPrecision(startsAt), Difficulty = 3, Status = NewsStoryStatus.Scheduled, Version = 2L },
            new { Network = edited.Network.NetworkId, Author = edited.Author.CharacterId, edited.Title, edited.Body, Start = edited.StartsAtUtc, Difficulty = edited.HackingDifficulty, edited.Status, edited.Version });
    }

    [Fact]
    public async Task StaleEditIsRejectedAndOverwritesNothing()
    {
        var network = await _data.CreateNetworkAsync();
        var author = await _data.CreateCharacterAsync();
        var content = _data.Content(network, author);
        var story = await _data.CreateAsync(content);
        await Stories.EditStoryAsync(story.StoryId, story.Version, content with { Title = "First edit" }, Token);

        var result = await Stories.EditStoryAsync(story.StoryId, story.Version, content with { Title = "Second edit" }, Token);

        var stored = await Stories.GetStoryAsync(story.StoryId, Token);
        Assert.Equal(new { Outcome = NewsOutcome.ConcurrentChange, Title = "First edit" }, new { result.Outcome, Title = stored!.Title });
    }

    [Fact]
    public async Task EditOfUnknownStoryIsNotFound()
    {
        var network = await _data.CreateNetworkAsync();
        var author = await _data.CreateCharacterAsync();

        var result = await Stories.EditStoryAsync(Guid.NewGuid(), 1, _data.Content(network, author), Token);

        Assert.Equal(NewsOutcome.NotFound, result.Outcome);
    }

    [Fact]
    public async Task EditCanKeepAnAuthorArchivedSince()
    {
        var network = await _data.CreateNetworkAsync();
        var author = await _data.CreateCharacterAsync();
        var content = _data.Content(network, author);
        var story = await _data.CreateAsync(content);
        await _data.ArchiveAsync(author);

        var result = await Stories.EditStoryAsync(story.StoryId, story.Version, content with { Title = "Updated" }, Token);

        Assert.Equal(NewsOutcome.Succeeded, result.Outcome);
    }

    [Fact]
    public async Task EditCanKeepANetworkDisabledSince()
    {
        await _data.CreateNetworkAsync();
        var network = await _data.CreateNetworkAsync("outer", "Outer rim");
        var author = await _data.CreateCharacterAsync();
        var content = _data.Content(network, author);
        var story = await _data.CreateAsync(content);
        await _data.Networks.DisableNetworkAsync(network.NetworkId, Token);

        var result = await Stories.EditStoryAsync(story.StoryId, story.Version, content with { Title = "Updated" }, Token);

        Assert.Equal(NewsOutcome.Succeeded, result.Outcome);
    }

    [Fact]
    public async Task EditCannotMoveAStoryToADisabledNetwork()
    {
        var network = await _data.CreateNetworkAsync();
        var disabled = await _data.CreateDisabledNetworkAsync();
        var author = await _data.CreateCharacterAsync();
        var content = _data.Content(network, author);
        var story = await _data.CreateAsync(content);

        var result = await Stories.EditStoryAsync(story.StoryId, story.Version, content with { NetworkId = disabled.NetworkId }, Token);

        Assert.Equal(NewsOutcome.NetworkDisabled, result.Outcome);
    }

    [Fact]
    public async Task EditCannotChooseAnArchivedAuthor()
    {
        var network = await _data.CreateNetworkAsync();
        var author = await _data.CreateCharacterAsync();
        var archived = await _data.CreateArchivedCharacterAsync();
        var content = _data.Content(network, author);
        var story = await _data.CreateAsync(content);

        var result = await Stories.EditStoryAsync(story.StoryId, story.Version, content with { AuthorId = archived.CharacterId }, Token);

        Assert.Equal(NewsOutcome.AuthorArchived, result.Outcome);
    }

    [Fact]
    public async Task ActiveAtModeListsOnlyStoriesActiveAtThatTime()
    {
        var network = await _data.CreateNetworkAsync();
        var author = await _data.CreateCharacterAsync();
        var now = factory.Clock.GetUtcNow();
        var active = await _data.CreateAsync(_data.Content(network, author) with { Title = "Active" });
        await _data.CreateAsync(_data.Content(network, author) with { Title = "Scheduled", StartsAt = now.AddHours(1), EndsAt = now.AddHours(2) });
        await _data.CreateAsync(_data.Content(network, author) with { Title = "Expired", StartsAt = now.AddHours(-2), EndsAt = now });

        var page = await Stories.GetStoriesAsync(new NewsStoryQuery(network.NetworkId, now, 1), Token);

        Assert.Equal(["Active"], page.Items.Select(story => story.Title));
    }

    [Fact]
    public async Task AllModeListsEveryStoryLatestStartFirstWithItsStatus()
    {
        var network = await _data.CreateNetworkAsync();
        var author = await _data.CreateCharacterAsync();
        var now = factory.Clock.GetUtcNow();
        await _data.CreateAsync(_data.Content(network, author) with { Title = "Active" });
        await _data.CreateAsync(_data.Content(network, author) with { Title = "Scheduled", StartsAt = now.AddHours(1), EndsAt = now.AddHours(2) });
        await _data.CreateAsync(_data.Content(network, author) with { Title = "Expired", StartsAt = now.AddHours(-3), EndsAt = now });

        var page = await Stories.GetStoriesAsync(new NewsStoryQuery(network.NetworkId, null, 1), Token);

        Assert.Equal(
            [("Scheduled", NewsStoryStatus.Scheduled), ("Active", NewsStoryStatus.Active), ("Expired", NewsStoryStatus.Expired)],
            page.Items.Select(story => (story.Title, story.Status)));
    }

    [Fact]
    public async Task ListShowsOnlyTheChosenNetwork()
    {
        var network = await _data.CreateNetworkAsync();
        var other = await _data.CreateNetworkAsync("outer", "Outer rim");
        var author = await _data.CreateCharacterAsync();
        await _data.CreateAsync(_data.Content(other, author));

        var page = await Stories.GetStoriesAsync(new NewsStoryQuery(network.NetworkId, null, 1), Token);

        Assert.Empty(page.Items);
    }

    [Fact]
    public async Task ListWithoutANetworkIsEmpty()
    {
        var network = await _data.CreateNetworkAsync();
        var author = await _data.CreateCharacterAsync();
        await _data.CreateAsync(_data.Content(network, author));

        var page = await Stories.GetStoriesAsync(new NewsStoryQuery(null, null, 1), Token);

        Assert.Equal(new { Items = 0, Pages = 1, Total = 0L }, new { Items = page.Items.Count, Pages = page.PageCount, Total = page.TotalCount });
    }

    [Fact]
    public async Task StoriesAreListedInPagesOfFifty()
    {
        var network = await _data.CreateNetworkAsync();
        var author = await _data.CreateCharacterAsync();
        for (var index = 0; index < 51; index++)
        {
            await _data.CreateAsync(_data.Content(network, author));
        }

        var page = await Stories.GetStoriesAsync(new NewsStoryQuery(network.NetworkId, null, 2), Token);

        Assert.Equal(new { Items = 1, Pages = 2, Total = 51L }, new { Items = page.Items.Count, Pages = page.PageCount, Total = page.TotalCount });
    }

    [Fact]
    public async Task ImageFromOutsideTheLibraryIsRejected()
    {
        var network = await _data.CreateNetworkAsync();
        var author = await _data.CreateCharacterAsync();

        var result = await Stories.CreateStoryAsync(Guid.NewGuid(),
            _data.Content(network, author, "![Logo](http://example.com/logo.png)"), Token);

        Assert.Equal(NewsOutcome.ExternalImage, result.Outcome);
    }

    [Fact]
    public async Task MediaNotInTheLibraryIsRejected()
    {
        var network = await _data.CreateNetworkAsync();
        var author = await _data.CreateCharacterAsync();

        var result = await Stories.CreateStoryAsync(Guid.NewGuid(),
            _data.Content(network, author, $"![Harbour](media:{Guid.NewGuid():D})"), Token);

        Assert.Equal(NewsOutcome.UnknownMedia, result.Outcome);
    }

    [Fact]
    public async Task VideoOptionsOnAPictureAreRejected()
    {
        var network = await _data.CreateNetworkAsync();
        var author = await _data.CreateCharacterAsync();
        var picture = await _data.UploadPictureAsync();

        var result = await Stories.CreateStoryAsync(Guid.NewGuid(),
            _data.Content(network, author, NewsTestData.Embed(picture, "autoplay")), Token);

        Assert.Equal(NewsOutcome.OptionsOnPicture, result.Outcome);
    }

    [Fact]
    public async Task VideoWithOptionsIsAccepted()
    {
        var network = await _data.CreateNetworkAsync();
        var author = await _data.CreateCharacterAsync();
        var video = await _data.UploadVideoAsync();

        var result = await Stories.CreateStoryAsync(Guid.NewGuid(),
            _data.Content(network, author, NewsTestData.Embed(video, "autoplay&loop&controls")), Token);

        Assert.Equal(NewsOutcome.Succeeded, result.Outcome);
    }

    [Fact]
    public async Task MoreThanTenPicturesAndVideosAreRejected()
    {
        var network = await _data.CreateNetworkAsync();
        var author = await _data.CreateCharacterAsync();
        var picture = await _data.UploadPictureAsync();
        var body = string.Join("\n\n", Enumerable.Repeat(NewsTestData.Embed(picture), 11));

        var result = await Stories.CreateStoryAsync(Guid.NewGuid(), _data.Content(network, author, body), Token);

        Assert.Equal(NewsOutcome.TooManyMedia, result.Outcome);
    }

    [Fact]
    public async Task TenPicturesAndVideosAreAccepted()
    {
        var network = await _data.CreateNetworkAsync();
        var author = await _data.CreateCharacterAsync();
        var picture = await _data.UploadPictureAsync();
        var body = string.Join("\n\n", Enumerable.Repeat(NewsTestData.Embed(picture), 10));

        var result = await Stories.CreateStoryAsync(Guid.NewGuid(), _data.Content(network, author, body), Token);

        Assert.Equal(NewsOutcome.Succeeded, result.Outcome);
    }

    [Fact]
    public async Task DetailsListTheMediaTheBodyUsesOnceInOrder()
    {
        var network = await _data.CreateNetworkAsync();
        var author = await _data.CreateCharacterAsync();
        var picture = await _data.UploadPictureAsync();
        var video = await _data.UploadVideoAsync();
        var body = $"{NewsTestData.Embed(video)}\n\n{NewsTestData.Embed(picture)}\n\n{NewsTestData.Embed(video, "loop")}";

        var story = await _data.CreateAsync(_data.Content(network, author, body));

        Assert.Equal([video.MediaId, picture.MediaId], story.Media.Select(media => media.MediaId));
    }

    [Fact]
    public async Task ArchivedAuthorIsMarked()
    {
        var network = await _data.CreateNetworkAsync();
        var author = await _data.CreateCharacterAsync("Kira");
        var story = await _data.CreateAsync(_data.Content(network, author));
        await _data.ArchiveAsync(author);

        var details = await Stories.GetStoryAsync(story.StoryId, Token);

        Assert.Equal(new NewsCharacter(author.CharacterId, true, author.Number, "Kira", true), details!.Author);
    }
}
