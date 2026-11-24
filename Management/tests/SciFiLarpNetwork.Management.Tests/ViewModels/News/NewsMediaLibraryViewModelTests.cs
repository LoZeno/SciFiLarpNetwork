using SciFiLarpNetwork.Management.ViewModels.News;

namespace SciFiLarpNetwork.Management.Tests.ViewModels.News;

public sealed class NewsMediaLibraryViewModelTests
{
    [Fact]
    public async Task LibraryStatesThePictureAndVideoLimits()
    {
        var library = await OpenLibraryAsync(new FakeNewsApi());

        Assert.Equal("Pictures: PNG, JPEG, WebP, GIF, up to 5 MB and 4096 × 4096 pixels. Videos: WebM, MP4, up to 100 MB.", library.LimitsText);
    }

    [Fact]
    public async Task ItalianLibraryStatesTheLimits()
    {
        var library = await OpenLibraryAsync(new FakeNewsApi(), "it");

        Assert.Equal("Immagini: PNG, JPEG, WebP, GIF, fino a 5 MB e 4096 × 4096 pixel. Video: WebM, MP4, fino a 100 MB.", library.LimitsText);
    }

    [Fact]
    public async Task ItemsShowNameKindTypeAndSize()
    {
        var api = new FakeNewsApi();
        api.AddMedia("Harbour", isVideo: false);
        api.AddMedia("Launch", isVideo: true);

        var library = await OpenLibraryAsync(api);

        Assert.Equal(
            ["Launch · Video · MP4 · 3 MB", "Harbour · Picture · PNG · 2 KB · 640 × 480"],
            library.Items.Select(item => item.DisplayName));
    }

    [Fact]
    public async Task SelectedPictureIsDownloadedForThePreview()
    {
        var api = new FakeNewsApi();
        api.AddMedia("Harbour", isVideo: false);
        var library = await OpenLibraryAsync(api);

        library.SelectedItem = library.Items.Single();
        await NewsScreens.WaitUntilAsync(() => library.SelectedPicture is not null);

        Assert.Equal(FakeNewsApi.PictureBytes, library.SelectedPicture);
    }

    [Fact]
    public async Task SelectedVideoIsNotDownloaded()
    {
        var api = new FakeNewsApi();
        api.AddMedia("Launch", isVideo: true);
        var library = await OpenLibraryAsync(api);

        library.SelectedItem = library.Items.Single();

        Assert.Equal(new { Video = true, Downloads = 0 }, new { Video = library.IsSelectedVideo, Downloads = api.ImageDownloads.Count });
    }

    [Fact]
    public async Task UploadUsesTheTypedNameAndSelectsTheNewItem()
    {
        var api = new FakeNewsApi();
        var library = await OpenLibraryAsync(api, dialogs: new FakeNewsDialogs { PickedFile = new PickedMediaFile("launch.mp4", [1, 2, 3]) });
        library.UploadName = "Launch day";

        await library.UploadCommand.ExecuteAsync();

        Assert.Equal(
            new { Upload = ("Launch day", "launch.mp4"), Selected = (string?)"Launch day" },
            new { Upload = api.Uploads.Single(), Selected = library.SelectedItem?.Media.Name });
    }

    [Fact]
    public async Task UploadWithoutANameUsesTheFileName()
    {
        var api = new FakeNewsApi();
        var library = await OpenLibraryAsync(api, dialogs: new FakeNewsDialogs { PickedFile = new PickedMediaFile("harbour.png", [1]) });

        await library.UploadCommand.ExecuteAsync();

        Assert.Equal("harbour", api.Uploads.Single().Name);
    }

    [Fact]
    public async Task FileLargerThanTheLargestVideoIsNotSent()
    {
        var api = new FakeNewsApi();
        var tooLarge = new byte[(100 * 1024 * 1024) + 1];
        var library = await OpenLibraryAsync(api, dialogs: new FakeNewsDialogs { PickedFile = new PickedMediaFile("big.mp4", tooLarge) });

        await library.UploadCommand.ExecuteAsync();

        Assert.Equal(
            new { Sent = 0, Status = (string?)"The file is too large: pictures can be at most 5 MB, videos at most 100 MB." },
            new { Sent = api.Uploads.Count, Status = library.StatusMessage });
    }

    [Fact]
    public async Task RemovalAsksFirstAndRemovesTheItem()
    {
        var api = new FakeNewsApi();
        api.AddMedia("Harbour", isVideo: false);
        var dialogs = new FakeNewsDialogs();
        var library = await OpenLibraryAsync(api, dialogs: dialogs);
        library.SelectedItem = library.Items.Single();

        await library.RemoveCommand.ExecuteAsync();

        Assert.Equal(
            new { Asked = (string?)"Remove \"Harbour\" from the library? Its file is deleted.", Left = 0 },
            new { Asked = dialogs.LastConfirmation?.Message, Left = library.Items.Count });
    }

    [Fact]
    public async Task MediaInUseIsReported()
    {
        var api = new FakeNewsApi();
        var picture = api.AddMedia("Harbour", isVideo: false);
        api.AddStory("Fleet", FakeNewsApi.CoreId, FakeNewsApi.Now, FakeNewsApi.Now.AddHours(1), $"![Harbour](media:{picture.Id:D})");
        var library = await OpenLibraryAsync(api);
        library.SelectedItem = library.Items.Single();

        await library.RemoveCommand.ExecuteAsync();

        Assert.Equal("A story uses this picture or video, so it cannot be removed.", library.StatusMessage);
    }

    [Fact]
    public async Task ReaderCannotUploadOrRemove()
    {
        var api = new FakeNewsApi { CanWrite = false };
        api.AddMedia("Harbour", isVideo: false);
        var library = await OpenLibraryAsync(api);
        library.SelectedItem = library.Items.Single();

        Assert.Equal(
            new { CanWrite = false, Upload = false, Remove = false },
            new { library.CanWrite, Upload = library.UploadCommand.CanExecute(null), Remove = library.RemoveCommand.CanExecute(null) });
    }

    [Fact]
    public async Task ChoosingAPictureIgnoresVideoOptions()
    {
        var api = new FakeNewsApi();
        api.AddMedia("Harbour", isVideo: false);
        var library = await OpenChoosingLibraryAsync(api);
        library.SelectedItem = library.Items.Single();
        library.Autoplay = true;

        library.ChooseCommand.Execute(null);

        Assert.Equal(Common.NewsMarkdown.NewsVideoOptions.None, library.Chosen?.Embed.Options);
    }

    [Fact]
    public async Task ChoosingWithoutTextIsRefused()
    {
        var api = new FakeNewsApi();
        api.AddMedia("Harbour", isVideo: false);
        var library = await OpenChoosingLibraryAsync(api);
        library.SelectedItem = library.Items.Single();
        library.MediaText = " ";

        library.ChooseCommand.Execute(null);

        Assert.Equal(
            new { Chosen = false, Status = (string?)"Every picture and video needs a text for people who cannot see it." },
            new { Chosen = library.Chosen is not null, Status = library.StatusMessage });
    }

    private static async Task<NewsMediaLibraryViewModel> OpenLibraryAsync(FakeNewsApi api, string locale = "en", FakeNewsDialogs? dialogs = null)
    {
        dialogs ??= new FakeNewsDialogs();
        var screen = await NewsScreens.OpenAsync(api, dialogs, locale);
        await screen.OpenMediaLibraryCommand.ExecuteAsync();
        return dialogs.LastLibrary!;
    }

    /// <summary>Opens the library from the story dialog's insert button, where it lets the user choose media.</summary>
    private static async Task<NewsMediaLibraryViewModel> OpenChoosingLibraryAsync(FakeNewsApi api)
    {
        var dialogs = new FakeNewsDialogs();
        var screen = await NewsScreens.OpenAsync(api, dialogs);
        await screen.NewStoryCommand.ExecuteAsync();
        await dialogs.LastEditor!.InsertMediaCommand.ExecuteAsync();
        return dialogs.LastLibrary!;
    }
}
