using SciFiLarpNetwork.Management.ViewModels.News.Preview;

namespace SciFiLarpNetwork.Management.Tests.ViewModels.News;

public sealed class NewsScreenViewModelTests
{
    private static readonly DateTimeOffset Now = FakeNewsApi.Now;

    [Fact]
    public async Task DefaultNetworkIsChosenFirst()
    {
        var screen = await NewsScreens.OpenAsync(new FakeNewsApi());

        Assert.Equal(FakeNewsApi.CoreId, screen.SelectedNetwork?.NetworkId);
    }

    [Fact]
    public async Task DisabledNetworksAreListedAndMarked()
    {
        var screen = await NewsScreens.OpenAsync(new FakeNewsApi());

        Assert.Equal(["Core", "Outer", "Old (Disabled)"], screen.Networks.Select(network => network.DisplayName));
    }

    [Fact]
    public async Task ScreenStartsListingStoriesActiveNow()
    {
        var api = new FakeNewsApi();
        api.AddStory("Active", FakeNewsApi.CoreId, Now.AddHours(-1), Now.AddHours(1));
        api.AddStory("Scheduled", FakeNewsApi.CoreId, Now.AddHours(1), Now.AddHours(2));
        api.AddStory("Expired", FakeNewsApi.CoreId, Now.AddHours(-2), Now.AddHours(-1));

        var screen = await NewsScreens.OpenAsync(api);

        Assert.Equal(["Active"], Titles(screen));
    }

    [Fact]
    public async Task AllModeListsScheduledAndExpiredStoriesWithTheirStatus()
    {
        var api = new FakeNewsApi();
        api.AddStory("Active", FakeNewsApi.CoreId, Now.AddHours(-1), Now.AddHours(1));
        api.AddStory("Scheduled", FakeNewsApi.CoreId, Now.AddHours(1), Now.AddHours(2));
        api.AddStory("Expired", FakeNewsApi.CoreId, Now.AddHours(-2), Now.AddHours(-1));
        var screen = await NewsScreens.OpenAsync(api);

        screen.ShowAll = true;
        await NewsScreens.WaitUntilAsync(() => screen.Stories.Count == 3);

        Assert.Equal(
            ["Scheduled · Kira-1 · Scheduled", "Active · Kira-1 · Active", "Expired · Kira-1 · Expired"],
            screen.Stories.Select(story => string.Join(" · ", story.DisplayName.Split(" · ")[1..]).Replace("#1 Kira", "Kira-1", StringComparison.Ordinal)));
    }

    [Fact]
    public async Task ActiveAtModeUsesTheChosenLocalDateAndTime()
    {
        var api = new FakeNewsApi();
        api.AddStory("Tomorrow", FakeNewsApi.CoreId, Now.AddDays(1), Now.AddDays(1).AddHours(2));
        var screen = await NewsScreens.OpenAsync(api, timeZone: NewsScreens.PlusOne);
        screen.ActiveAtDate = new DateTime(2026, 11, 25);
        screen.ActiveAtTime = new TimeSpan(13, 30, 0);

        await screen.ApplyFilterCommand.ExecuteAsync();

        Assert.Equal(["Tomorrow"], Titles(screen));
    }

    [Fact]
    public async Task ActiveAtFilterSendsTheInstantInUtc()
    {
        var api = new FakeNewsApi();
        var screen = await NewsScreens.OpenAsync(api, timeZone: NewsScreens.PlusOne);
        screen.ActiveAtDate = new DateTime(2026, 11, 25);
        screen.ActiveAtTime = new TimeSpan(13, 30, 0);

        await screen.ApplyFilterCommand.ExecuteAsync();

        Assert.Contains("activeAt=2026-11-25T12%3A30%3A00.0000000%2B00%3A00", api.ListLoads[^1], StringComparison.Ordinal);
    }

    [Fact]
    public async Task ChoosingANetworkListsItsStories()
    {
        var api = new FakeNewsApi();
        api.AddStory("Core story", FakeNewsApi.CoreId, Now.AddHours(-1), Now.AddHours(1));
        api.AddStory("Outer story", FakeNewsApi.OuterId, Now.AddHours(-1), Now.AddHours(1));
        var screen = await NewsScreens.OpenAsync(api);

        screen.SelectedNetwork = screen.Networks.Single(network => network.NetworkId == FakeNewsApi.OuterId);
        await NewsScreens.WaitUntilAsync(() => Titles(screen).SequenceEqual(["Outer story"]));

        Assert.Equal(["Outer story"], Titles(screen));
    }

    [Fact]
    public async Task EmptyListSaysNoStoryIsActive()
    {
        var screen = await NewsScreens.OpenAsync(new FakeNewsApi());

        Assert.Equal("No stories are active at this time.", screen.StoriesPlaceholder);
    }

    [Fact]
    public async Task StoriesArePagedByFifty()
    {
        var api = new FakeNewsApi();
        foreach (var number in Enumerable.Range(1, 51))
        {
            api.AddStory($"Story {number}", FakeNewsApi.CoreId, Now.AddHours(-1).AddMinutes(number), Now.AddHours(1));
        }

        var screen = await NewsScreens.OpenAsync(api);
        await screen.NextStoriesCommand.ExecuteAsync();

        Assert.Equal(new { Count = 1, Page = "Page 2 of 2 (51 in total)" }, new { screen.Stories.Count, Page = screen.StoryPage });
    }

    [Fact]
    public async Task DetailsShowTheStoryInTheScreensTimeZone()
    {
        var api = new FakeNewsApi();
        api.AddStory("Fleet arrives", FakeNewsApi.CoreId, Now.AddHours(-1), Now.AddHours(1));
        var screen = await NewsScreens.OpenAsync(api, timeZone: NewsScreens.PlusOne);

        await NewsScreens.SelectAsync(screen, "Fleet arrives");

        Assert.Equal(
            new
            {
                Author = "By: #1 Kira",
                Network = "Network: Core",
                Times = $"From {new DateTime(2026, 11, 24, 12, 0, 0):g} to {new DateTime(2026, 11, 24, 14, 0, 0):g}",
                Status = "Active",
                Difficulty = "Hacking difficulty: 2"
            },
            new
            {
                Author = screen.DetailAuthor,
                Network = screen.DetailNetwork,
                Times = screen.DetailTimes,
                Status = screen.DetailStatus,
                Difficulty = screen.DetailHackingDifficulty
            });
    }

    [Fact]
    public async Task DetailsNameTheTimeZone()
    {
        var screen = await NewsScreens.OpenAsync(new FakeNewsApi(), timeZone: NewsScreens.PlusOne);

        Assert.Equal("Times are in Test +1.", screen.TimeZoneText);
    }

    [Fact]
    public async Task DetailsPreviewShowsPicturesAndVideosOfTheStory()
    {
        var api = new FakeNewsApi();
        var picture = api.AddMedia("Harbour", isVideo: false);
        var video = api.AddMedia("Launch", isVideo: true);
        api.AddStory("Fleet arrives", FakeNewsApi.CoreId, Now.AddHours(-1), Now.AddHours(1),
            $"Intro\n\n![Harbour at dawn](media:{picture.Id:D})\n\n![Launch day](media:{video.Id:D}?autoplay&controls)");
        var screen = await NewsScreens.OpenAsync(api);

        await NewsScreens.SelectAsync(screen, "Fleet arrives");

        Assert.Equal(
            ["text:Intro", "picture:Harbour at dawn:7 bytes", "video:Launch day:Launch · video/mp4 · 3 MB · Autoplay · Controls"],
            screen.DetailPreview.Select(Describe));
    }

    [Fact]
    public async Task ReaderSeesNoNewOrEditCommands()
    {
        var api = new FakeNewsApi { CanWrite = false };
        api.AddStory("Fleet arrives", FakeNewsApi.CoreId, Now.AddHours(-1), Now.AddHours(1));
        var screen = await NewsScreens.OpenAsync(api);
        await NewsScreens.SelectAsync(screen, "Fleet arrives");

        Assert.Equal(
            new { CanWrite = false, New = false, Edit = false, Library = true },
            new
            {
                screen.CanWrite,
                New = screen.NewStoryCommand.CanExecute(null),
                Edit = screen.EditStoryCommand.CanExecute(null),
                Library = screen.OpenMediaLibraryCommand.CanExecute(null)
            });
    }

    [Fact]
    public async Task ItalianLabelsAreShown()
    {
        var screen = await NewsScreens.OpenAsync(new FakeNewsApi(), locale: "it");

        Assert.Equal(
            new { Heading = "Notizie", All = "Tutte le notizie", New = "Nuova", Library = "Libreria dei media" },
            new { Heading = screen.NewsHeading, All = screen.AllLabel, New = screen.NewLabel, Library = screen.MediaLibraryLabel });
    }

    private static string[] Titles(Management.ViewModels.News.NewsScreenViewModel screen) =>
        [.. screen.Stories.Select(story => story.DisplayName.Split(" · ")[1])];

    private static string Describe(NewsPreviewBlock block) => block switch
    {
        NewsPreviewText text => $"text:{text.PlainText}",
        NewsPreviewPicture picture => $"picture:{picture.Text}:{picture.Content?.Length} bytes",
        NewsPreviewVideo video => $"video:{video.Text}:{video.Description}",
        NewsPreviewProblem problem => $"problem:{problem.Message}",
        _ => block.GetType().Name
    };
}
