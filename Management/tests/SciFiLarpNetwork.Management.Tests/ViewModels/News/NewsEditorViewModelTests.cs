using System.Net;
using System.Text.Json;
using SciFiLarpNetwork.Management.Api;
using SciFiLarpNetwork.Management.ViewModels.News;
using SciFiLarpNetwork.Management.ViewModels.News.Preview;

namespace SciFiLarpNetwork.Management.Tests.ViewModels.News;

public sealed class NewsEditorViewModelTests
{
    private static readonly DateTimeOffset Now = FakeNewsApi.Now;

    [Fact]
    public async Task NewStoryStartsNowOnTheListedNetworkAndEndsADayLater()
    {
        NewsEditorViewModel? shown = null;
        var dialogs = new FakeNewsDialogs { OnStoryEditor = editor => Task.FromResult(shown = editor) };
        var screen = await NewsScreens.OpenAsync(new FakeNewsApi(), dialogs, timeZone: NewsScreens.PlusOne);

        await screen.NewStoryCommand.ExecuteAsync();

        Assert.Equal(
            new { Network = (Guid?)FakeNewsApi.CoreId, StartDate = (DateTime?)new DateTime(2026, 11, 24), StartTime = (TimeSpan?)new TimeSpan(13, 0, 0), EndDate = (DateTime?)new DateTime(2026, 11, 25), Difficulty = (decimal?)1 },
            new { Network = shown!.Network?.NetworkId, shown.StartDate, shown.StartTime, shown.EndDate, Difficulty = shown.HackingDifficulty });
    }

    [Fact]
    public async Task EditorOffersEnabledNetworksOnly()
    {
        var dialogs = new FakeNewsDialogs();
        var screen = await NewsScreens.OpenAsync(new FakeNewsApi(), dialogs);

        await screen.NewStoryCommand.ExecuteAsync();

        Assert.Equal(["Core", "Outer"], dialogs.LastEditor!.Networks.Select(network => network.DisplayName));
    }

    [Fact]
    public async Task SavingANewStorySendsItsValuesWithTimesInUtc()
    {
        var api = new FakeNewsApi();
        var dialogs = new FakeNewsDialogs { OnStoryEditor = editor => WriteAsync(editor, "Fleet arrives", "**Tonight**") };
        var screen = await NewsScreens.OpenAsync(api, dialogs, timeZone: NewsScreens.PlusOne);

        await screen.NewStoryCommand.ExecuteAsync();

        var body = JsonDocument.Parse(api.Submissions.Single().Body!).RootElement;
        Assert.Equal(
            new { Title = (string?)"Fleet arrives", Body = (string?)"**Tonight**", Author = FakeNewsApi.OdoId, Start = (string?)"2026-11-24T12:00:00+00:00", Difficulty = 3 },
            new
            {
                Title = body.GetProperty("title").GetString(),
                Body = body.GetProperty("body").GetString(),
                Author = body.GetProperty("authorId").GetGuid(),
                Start = body.GetProperty("startsAt").GetString(),
                Difficulty = body.GetProperty("hackingDifficulty").GetInt32()
            });
    }

    [Fact]
    public async Task SavedStoryIsListedAndShown()
    {
        var api = new FakeNewsApi();
        var dialogs = new FakeNewsDialogs { OnStoryEditor = editor => WriteAsync(editor, "Fleet arrives", "Tonight.") };
        var screen = await NewsScreens.OpenAsync(api, dialogs);

        await screen.NewStoryCommand.ExecuteAsync();

        Assert.Equal(
            new { Listed = (string?)"Fleet arrives", Shown = "Fleet arrives", Status = (string?)"Action completed." },
            new { Listed = screen.SelectedStory?.DisplayName.Split(" · ")[1], Shown = screen.DetailTitle, Status = screen.StatusMessage });
    }

    [Fact]
    public async Task SavingAgainAfterAFailureSendsTheSameStoryId()
    {
        var api = new FakeNewsApi();
        api.Failures["create-news-story"] = new HttpRequestException("Lost answer");
        var dialogs = new FakeNewsDialogs
        {
            OnStoryEditor = async editor =>
            {
                await WriteAsync(editor, "Fleet arrives", "Tonight.");
                await editor.SaveCommand.ExecuteAsync();
            }
        };
        var screen = await NewsScreens.OpenAsync(api, dialogs);

        await screen.NewStoryCommand.ExecuteAsync();

        var ids = api.Submissions.Select(submission => JsonDocument.Parse(submission.Body!).RootElement.GetProperty("storyId").GetGuid()).Distinct();
        Assert.Single(ids);
    }

    [Fact]
    public async Task MissingAuthorIsReportedWithoutSaving()
    {
        var api = new FakeNewsApi();
        var dialogs = new FakeNewsDialogs
        {
            OnStoryEditor = async editor =>
            {
                editor.StoryTitle = "Fleet arrives";
                editor.Body = "Tonight.";
                await editor.SaveCommand.ExecuteAsync();
            }
        };
        var screen = await NewsScreens.OpenAsync(api, dialogs);

        await screen.NewStoryCommand.ExecuteAsync();

        Assert.Equal(new { Error = (string?)"Choose an author.", Sent = 0 }, new { Error = dialogs.LastEditor!.ErrorMessage, Sent = api.Submissions.Count });
    }

    [Fact]
    public async Task DifficultyAboveTheLargestWholeNumberIsReportedWithoutSaving()
    {
        var api = new FakeNewsApi();
        var dialogs = new FakeNewsDialogs
        {
            OnStoryEditor = async editor =>
            {
                await WriteAsync(editor, "Fleet arrives", "Tonight.", difficulty: 3_000_000_000m);
            }
        };
        var screen = await NewsScreens.OpenAsync(api, dialogs);

        await screen.NewStoryCommand.ExecuteAsync();

        Assert.Equal(
            new { Error = (string?)"The hacking difficulty must be a whole number of 1 or more.", Sent = 0 },
            new { Error = dialogs.LastEditor!.ErrorMessage, Sent = api.Submissions.Count });
    }

    [Fact]
    public async Task ServerErrorIsShownInTheDialog()
    {
        var api = new FakeNewsApi();
        api.Failures["create-news-story"] = new ManagementApiException("errors.invalidNewsTimes", HttpStatusCode.BadRequest);
        var dialogs = new FakeNewsDialogs { OnStoryEditor = editor => WriteAsync(editor, "Fleet arrives", "Tonight.") };
        var screen = await NewsScreens.OpenAsync(api, dialogs);

        await screen.NewStoryCommand.ExecuteAsync();

        Assert.Equal("The end must come after the start.", dialogs.LastEditor!.ErrorMessage);
    }

    [Fact]
    public async Task EditStartsWithTheStorysValues()
    {
        var api = new FakeNewsApi();
        api.AddStory("Fleet arrives", FakeNewsApi.OldId, Now.AddHours(-1), Now.AddHours(1), "Tonight.");
        var dialogs = new FakeNewsDialogs();
        var screen = await NewsScreens.OpenAsync(api, dialogs);
        screen.SelectedNetwork = screen.Networks.Single(network => network.NetworkId == FakeNewsApi.OldId);
        await NewsScreens.WaitUntilAsync(() => screen.Stories.Count == 1);
        await NewsScreens.SelectAsync(screen, "Fleet arrives");

        await screen.EditStoryCommand.ExecuteAsync();

        var editor = dialogs.LastEditor!;
        Assert.Equal(
            new { Network = (Guid?)FakeNewsApi.OldId, Title = "Fleet arrives", Body = "Tonight.", Author = (Guid?)FakeNewsApi.KiraId, Difficulty = (decimal?)2, Version = (long?)1 },
            new { Network = editor.Network?.NetworkId, Title = editor.StoryTitle, editor.Body, Author = editor.Author?.CharacterId, Difficulty = editor.HackingDifficulty, editor.Version });
    }

    [Fact]
    public async Task EditOfAStoryOnADisabledNetworkStillOffersThatNetwork()
    {
        var api = new FakeNewsApi();
        api.AddStory("Fleet arrives", FakeNewsApi.OldId, Now.AddHours(-1), Now.AddHours(1));
        var dialogs = new FakeNewsDialogs();
        var screen = await NewsScreens.OpenAsync(api, dialogs);
        screen.SelectedNetwork = screen.Networks.Single(network => network.NetworkId == FakeNewsApi.OldId);
        await NewsScreens.WaitUntilAsync(() => screen.Stories.Count == 1);
        await NewsScreens.SelectAsync(screen, "Fleet arrives");

        await screen.EditStoryCommand.ExecuteAsync();

        Assert.Equal(["Core", "Outer", "Old (Disabled)"], dialogs.LastEditor!.Networks.Select(network => network.DisplayName));
    }

    [Fact]
    public async Task ConcurrentChangeKeepsTheInputAndBasesTheNextSaveOnTheStoredVersion()
    {
        var api = new FakeNewsApi();
        var story = api.AddStory("Fleet arrives", FakeNewsApi.CoreId, Now.AddHours(-1), Now.AddHours(1));
        var dialogs = new FakeNewsDialogs
        {
            OnStoryEditor = async editor =>
            {
                api.Stories[0] = story with { Title = "Changed by someone else", Version = 2 };
                api.Failures["edit-news-story"] = new ManagementApiException("errors.concurrentChange", HttpStatusCode.Conflict);
                editor.StoryTitle = "My title";
                await editor.SaveCommand.ExecuteAsync();
            }
        };
        var screen = await NewsScreens.OpenAsync(api, dialogs);
        await NewsScreens.SelectAsync(screen, "Fleet arrives");

        await screen.EditStoryCommand.ExecuteAsync();

        var editor = dialogs.LastEditor!;
        Assert.Equal(
            new { Title = "My title", Version = (long?)2, Error = (string?)"Someone saved a related change at the same time. Your input is kept: save again." },
            new { Title = editor.StoryTitle, editor.Version, Error = editor.ErrorMessage });
    }

    [Fact]
    public async Task ConcurrentChangeOnANewStoryKeepsItNewWhileAnotherStoryIsSelected()
    {
        var api = new FakeNewsApi();
        api.AddStory("Fleet arrives", FakeNewsApi.CoreId, Now.AddHours(-1), Now.AddHours(1));
        var dialogs = new FakeNewsDialogs
        {
            OnStoryEditor = async editor =>
            {
                api.Failures["create-news-story"] = new ManagementApiException("errors.concurrentChange", HttpStatusCode.Conflict);
                await WriteAsync(editor, "Fleet delayed", "Not tonight.");
                await editor.SaveCommand.ExecuteAsync();
            }
        };
        var screen = await NewsScreens.OpenAsync(api, dialogs);
        await NewsScreens.SelectAsync(screen, "Fleet arrives");

        await screen.NewStoryCommand.ExecuteAsync();

        Assert.Equal(
            new { IsEdit = false, Saved = true, Stories = 2 },
            new { dialogs.LastEditor!.IsEdit, Saved = dialogs.LastEditor.IsSaved, Stories = api.Stories.Count });
    }

    [Fact]
    public async Task PreviewFollowsTheTypedText()
    {
        var dialogs = new FakeNewsDialogs();
        var screen = await NewsScreens.OpenAsync(new FakeNewsApi(), dialogs);
        await screen.NewStoryCommand.ExecuteAsync();

        dialogs.LastEditor!.Body = "## Fleet\n\n**Tonight** at the dock.";

        Assert.Equal(
            [new NewsPreviewText([new NewsPreviewInline("Fleet")], 2, string.Empty, 0).PlainText, "Tonight at the dock."],
            dialogs.LastEditor.Preview.OfType<NewsPreviewText>().Select(text => text.PlainText));
    }

    [Fact]
    public async Task InsertedMediaIsWrittenAtTheCaretAsItsOwnParagraph()
    {
        var api = new FakeNewsApi();
        var video = api.AddMedia("Launch", isVideo: true);
        var dialogs = new FakeNewsDialogs();
        dialogs.OnMediaLibrary = library =>
        {
            library.SelectedItem = library.Items.Single();
            library.MediaText = "Launch day";
            library.Autoplay = true;
            library.ChooseCommand.Execute(null);
            return Task.CompletedTask;
        };
        var screen = await NewsScreens.OpenAsync(api, dialogs);
        await screen.NewStoryCommand.ExecuteAsync();
        var editor = dialogs.LastEditor!;
        editor.Body = "Before.";
        editor.SelectionStart = editor.SelectionEnd = 7;

        await editor.InsertMediaCommand.ExecuteAsync();

        Assert.Equal($"Before.\n\n![Launch day](media:{video.Id:D}?autoplay&controls)", editor.Body);
    }

    [Fact]
    public async Task TypingWhileAPictureDownloadsAsksForItOnce()
    {
        var api = new FakeNewsApi();
        var picture = api.AddMedia("Harbour", isVideo: false);
        var dialogs = new FakeNewsDialogs();
        var screen = await NewsScreens.OpenAsync(api, dialogs);
        await screen.OpenMediaLibraryCommand.ExecuteAsync();
        await screen.NewStoryCommand.ExecuteAsync();
        var editor = dialogs.LastEditor!;
        api.HoldImageDownloads = true;

        editor.Body = $"![Harbour](media:{picture.Id:D})";
        editor.Body += "\n\nMore";
        editor.Body += " text";
        api.ReleaseImageDownloads();

        Assert.Single(api.ImageDownloads);
    }

    [Fact]
    public async Task PictureThatFailedIsNotAskedForAgainWhileTyping()
    {
        var api = new FakeNewsApi { FailImageDownloads = true };
        var picture = api.AddMedia("Harbour", isVideo: false);
        var dialogs = new FakeNewsDialogs();
        var screen = await NewsScreens.OpenAsync(api, dialogs);
        await screen.OpenMediaLibraryCommand.ExecuteAsync();
        await screen.NewStoryCommand.ExecuteAsync();
        var editor = dialogs.LastEditor!;

        editor.Body = $"![Harbour](media:{picture.Id:D})";
        editor.Body += "\n\nMore";
        editor.Body += " text";

        Assert.Single(api.ImageDownloads);
    }

    [Fact]
    public async Task MediaCountShowsTheLimit()
    {
        var api = new FakeNewsApi();
        var picture = api.AddMedia("Harbour", isVideo: false);
        var dialogs = new FakeNewsDialogs();
        var screen = await NewsScreens.OpenAsync(api, dialogs);
        await screen.NewStoryCommand.ExecuteAsync();

        dialogs.LastEditor!.Body = $"![Harbour](media:{picture.Id:D})";

        Assert.Equal("Pictures and videos: 1 of 10", dialogs.LastEditor.MediaCountText);
    }

    [Fact]
    public async Task BoldWrapsTheSelection()
    {
        var dialogs = new FakeNewsDialogs();
        var screen = await NewsScreens.OpenAsync(new FakeNewsApi(), dialogs);
        await screen.NewStoryCommand.ExecuteAsync();
        var editor = dialogs.LastEditor!;
        editor.Body = "Fleet arrives";
        editor.SelectionStart = 6;
        editor.SelectionEnd = 13;

        editor.BoldCommand.Execute(null);

        Assert.Equal("Fleet **arrives**", editor.Body);
    }

    [Fact]
    public async Task ItalianDialogLabelsAreShown()
    {
        var dialogs = new FakeNewsDialogs();
        var screen = await NewsScreens.OpenAsync(new FakeNewsApi(), dialogs, locale: "it");

        await screen.NewStoryCommand.ExecuteAsync();

        var editor = dialogs.LastEditor!;
        Assert.Equal(
            new { Title = "Nuova notizia", Insert = "Inserisci immagine o video", Difficulty = "Difficoltà di hacking" },
            new { editor.Title, Insert = editor.InsertMediaLabel, Difficulty = editor.HackingDifficultyLabel });
    }

    private static async Task WriteAsync(NewsEditorViewModel editor, string title, string body, decimal difficulty = 3)
    {
        editor.SearchText = "Odo";
        await editor.SearchCommand.ExecuteAsync();
        editor.SetAuthorCommand.Execute(null);
        editor.StoryTitle = title;
        editor.Body = body;
        editor.HackingDifficulty = difficulty;
        await editor.SaveCommand.ExecuteAsync();
    }
}
