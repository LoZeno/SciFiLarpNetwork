using Avalonia.Controls;
using Avalonia.Threading;
using Avalonia.VisualTree;
using SciFiLarpNetwork.Management.Localization;
using SciFiLarpNetwork.Management.Settings;
using SciFiLarpNetwork.Management.Tests.Application;
using SciFiLarpNetwork.Management.Tests.ViewModels.CardTemplates;
using SciFiLarpNetwork.Management.Tests.ViewModels.LarpEvents;
using SciFiLarpNetwork.Management.Tests.ViewModels.Messages;
using SciFiLarpNetwork.Management.Tests.ViewModels.Networks;
using SciFiLarpNetwork.Management.Tests.ViewModels.News;
using SciFiLarpNetwork.Management.Tests.ViewModels.Players;
using SciFiLarpNetwork.Management.Tests.ViewModels.Skills;
using SciFiLarpNetwork.Management.ViewModels;
using SciFiLarpNetwork.Management.ViewModels.News;
using SciFiLarpNetwork.Management.Views;
using SciFiLarpNetwork.Management.Views.News;

namespace SciFiLarpNetwork.Management.Tests.Views;

public sealed class NewsWindowTests
{
    private static readonly DateTimeOffset Now = DateTimeOffset.UtcNow;
    private static readonly string[] ToolbarLabels = ["Bold", "Italic", "Heading", "Bulleted list", "Numbered list", "Insert picture or video"];

    [Fact]
    public Task MainWindowShowsTheNewsScreenWhenItsEntryIsSelected() => AvaloniaUi.Run(async () =>
    {
        var viewModel = CreateViewModel(new FakeNewsApi());
        var window = new MainWindow(viewModel);
        window.Show();
        await viewModel.ConnectCommand.ExecuteAsync();

        viewModel.SelectedNavigationItem = viewModel.NavigationItems[0];
        Dispatcher.UIThread.RunJobs();

        Assert.True(window.GetVisualDescendants().OfType<NewsScreenView>().Single().IsEffectivelyVisible);
        window.Close();
    });

    [Fact]
    public Task SelectedStoryIsDrawnWithItsFormattedText() => AvaloniaUi.Run(async () =>
    {
        var api = new FakeNewsApi();
        api.AddStory("Fleet arrives", FakeNewsApi.CoreId, Now.AddHours(-1), Now.AddHours(1), "**Tonight** at the dock.");
        var viewModel = CreateViewModel(api);
        var window = new MainWindow(viewModel);
        window.Show();
        await viewModel.ConnectCommand.ExecuteAsync();
        viewModel.SelectedNavigationItem = viewModel.NavigationItems[0];

        viewModel.News!.SelectedStory = viewModel.News.Stories[0];
        Dispatcher.UIThread.RunJobs();

        var text = window.GetVisualDescendants().OfType<FormattedTextBlock>().Single();
        Assert.Equal(
            new { Text = (string?)"Tonight at the dock.", FirstIsBold = true },
            new { Text = text.Inlines?.Text, FirstIsBold = text.Inlines![0].FontWeight == Avalonia.Media.FontWeight.Bold });
        window.Close();
    });

    [Fact]
    public Task EditorWindowShowsTheToolbarAndThePreview() => AvaloniaUi.Run(async () =>
    {
        var dialogs = new FakeNewsDialogs();
        var screen = await NewsScreens.OpenAsync(new FakeNewsApi(), dialogs);
        await screen.NewStoryCommand.ExecuteAsync();
        dialogs.LastEditor!.Body = "# Fleet";
        var window = new NewsEditorWindow(dialogs.LastEditor);
        window.Show();
        Dispatcher.UIThread.RunJobs();

        var buttons = window.GetVisualDescendants().OfType<Button>().Select(button => button.Content as string).ToArray();
        var preview = window.GetVisualDescendants().OfType<NewsPreviewView>().Single();
        Assert.Equal(
            new { Toolbar = true, Previewed = (string?)"Fleet" },
            new
            {
                Toolbar = ToolbarLabels.All(buttons.Contains),
                Previewed = preview.GetVisualDescendants().OfType<FormattedTextBlock>().Single().Inlines?.Text
            });
        window.Close();
    });

    [Fact]
    public Task MediaLibraryWindowStatesTheSizeLimits() => AvaloniaUi.Run(async () =>
    {
        var dialogs = new FakeNewsDialogs();
        var screen = await NewsScreens.OpenAsync(new FakeNewsApi(), dialogs);
        await screen.OpenMediaLibraryCommand.ExecuteAsync();
        var window = new NewsMediaLibraryWindow(dialogs.LastLibrary!);
        window.Show();
        Dispatcher.UIThread.RunJobs();

        var texts = window.GetVisualDescendants().OfType<TextBlock>().Select(block => block.Text).ToArray();
        Assert.Contains("Pictures: PNG, JPEG, WebP, GIF, up to 5 MB and 4096 × 4096 pixels. Videos: WebM, MP4, up to 100 MB.", texts);
        window.Close();
    });

    [Fact]
    public Task SelectedVideoShowsAPlaySymbolAndNote() => AvaloniaUi.Run(async () =>
    {
        var api = new FakeNewsApi();
        api.AddMedia("Launch", isVideo: true);
        var dialogs = new FakeNewsDialogs();
        var screen = await NewsScreens.OpenAsync(api, dialogs);
        await screen.OpenMediaLibraryCommand.ExecuteAsync();
        var library = dialogs.LastLibrary!;
        library.SelectedItem = library.Items.Single();
        var window = new NewsMediaLibraryWindow(library);
        window.Show();
        Dispatcher.UIThread.RunJobs();

        var visibleTexts = window.GetVisualDescendants().OfType<TextBlock>().Where(block => block.IsEffectivelyVisible).Select(block => block.Text).ToArray();
        Assert.Equal(
            new { Symbol = true, Note = true },
            new
            {
                Symbol = visibleTexts.Contains("▶"),
                Note = visibleTexts.Contains("Videos are not played here. Players see them in the news feed.")
            });
        window.Close();
    });

    private static MainWindowViewModel CreateViewModel(FakeNewsApi api) =>
        new(api, new NullSettingsStore(), new JsonLocalizer(),
            new FakeCatalogDialogs(), new FakeNetworkDialogs(), new FakePlayerDialogs(), new FakeCardTemplateDialogs(),
            new FakeLarpEventDialogs(), new FakeMessageDialogs(), new FakeNewsDialogs(), new SciFiLarpNetwork.Management.Tests.ViewModels.Items.FakeItemDialogs(), new ManagementSettings());

    private sealed class NullSettingsStore : IManagementSettingsStore
    {
        public ManagementSettings Load() => new();

        public void Save(ManagementSettings settings)
        {
        }
    }
}
