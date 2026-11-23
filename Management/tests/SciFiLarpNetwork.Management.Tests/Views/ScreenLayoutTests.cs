using SciFiLarpNetwork.Management.Tests.Application;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.Presenters;
using Avalonia.Threading;
using Avalonia.VisualTree;
using SciFiLarpNetwork.Management.Api;
using SciFiLarpNetwork.Management.Localization;
using SciFiLarpNetwork.Management.Settings;
using SciFiLarpNetwork.Management.Tests.ViewModels.CardTemplates;
using SciFiLarpNetwork.Management.Tests.ViewModels.LarpEvents;
using SciFiLarpNetwork.Management.Tests.ViewModels.Networks;
using SciFiLarpNetwork.Management.Tests.ViewModels.Players;
using SciFiLarpNetwork.Management.Tests.ViewModels.Skills;
using SciFiLarpNetwork.Management.ViewModels;
using SciFiLarpNetwork.Management.Views;

namespace SciFiLarpNetwork.Management.Tests.Views;

/// <summary>
/// Opens every screen in every language, at each of <see cref="WindowSizes"/>, and checks that no control sticks out of
/// the control that holds it. Screens come from <see cref="NavigationScreen"/>, so a new screen fails
/// here until it has a fake API below.
/// </summary>
public sealed class ScreenLayoutTests
{
    private const double Tolerance = 0.5;

    /// <summary>The default window, a 1568x1087 pixel window at 175% scaling, and the smallest window allowed.</summary>
    private static readonly (double Width, double Height)[] WindowSizes = [(1080, 720), (896, 621), (720, 480)];

    private static readonly string[] Locales = ["en", "it"];

    public static TheoryData<NavigationScreen, string, double, double> Cases()
    {
        var cases = new TheoryData<NavigationScreen, string, double, double>();
        foreach (var screen in Enum.GetValues<NavigationScreen>().Where(screen => screen != NavigationScreen.ResourceSummary))
        {
            foreach (var locale in Locales)
            {
                foreach (var (width, height) in WindowSizes)
                {
                    cases.Add(screen, locale, width, height);
                }
            }
        }

        return cases;
    }

    [Theory]
    [MemberData(nameof(Cases))]
    public Task NoControlSticksOutOfItsContainer(NavigationScreen screen, string locale, double width, double height) => AvaloniaUi.Run(async () =>
    {
        var window = await ShowScreenAsync(screen, locale, width, height);
        var view = ScreenView(window);

        var overflowing = view.GetVisualDescendants().OfType<Control>()
            .Where(control => control.IsEffectivelyVisible && !IsScrolledContent(view, control) && !IsInsideTextField(control))
            .Where(control => SticksOutOfItsParent(control) || IsCutOffText(control) || DrawsTextOutsideItself(control))
            .Select(Describe)
            .ToArray();

        window.Close();
        Assert.Empty(overflowing);
    });

    [Fact]
    public Task SmallWindowScrollsTheScreen() => AvaloniaUi.Run(async () =>
    {
        var window = await ShowScreenAsync(NavigationScreen.PlayerRoster, "it", 720, 480);
        var scroller = ScreenScroller(window);

        window.Close();
        Assert.True(Scrolls(scroller));
    });

    [Fact]
    public Task LongListScrollsByItselfWhenTheScreenFits() => AvaloniaUi.Run(async () =>
    {
        var api = new FakePlayerApi();
        foreach (var number in Enumerable.Range(1, 40))
        {
            api.AddPlayer($"Player {number}");
        }

        var window = await ShowScreenAsync(api, NavigationScreen.PlayerRoster, "en", 1080, 720);
        var playerList = ScreenView(window).GetVisualDescendants().OfType<ListBox>().First();
        var listScroller = playerList.GetVisualDescendants().OfType<ScrollViewer>().First();

        window.Close();
        Assert.Equal(
            new { ScreenScrolls = false, ListScrolls = true },
            new { ScreenScrolls = Scrolls(ScreenScroller(window)), ListScrolls = Scrolls(listScroller) });
    });

    private static ScrollViewer ScreenScroller(MainWindow window) => ScreenView(window).FindAncestorOfType<ScrollViewer>()!;

    private static bool Scrolls(ScrollViewer scroller) => scroller.Extent.Height > scroller.Viewport.Height + Tolerance;

    private static Task<MainWindow> ShowScreenAsync(NavigationScreen screen, string locale, double width, double height) =>
        ShowScreenAsync(CreateApi(screen), screen, locale, width, height);

    private static async Task<MainWindow> ShowScreenAsync(
        IManagementApiClient api, NavigationScreen screen, string locale, double width, double height)
    {
        var viewModel = new MainWindowViewModel(api, new NullSettingsStore(), new JsonLocalizer(),
            new FakeCatalogDialogs(), new FakeNetworkDialogs(), new FakePlayerDialogs(), new FakeCardTemplateDialogs(),
            new FakeLarpEventDialogs(), new ManagementSettings(Locale: locale));
        var window = new MainWindow(viewModel) { Width = width, Height = height };
        window.Show();
        await viewModel.ConnectCommand.ExecuteAsync();
        viewModel.SelectedNavigationItem = viewModel.NavigationItems.Single(item => item.Screen == screen);
        Settle(window);
        SelectFirstEntryOfEveryList(ScreenView(window));
        Settle(window);
        return window;
    }

    private static IManagementApiClient CreateApi(NavigationScreen screen) => screen switch
    {
        NavigationScreen.TalentCatalog => TalentCatalogApi(),
        NavigationScreen.NetworkCatalog => NetworkCatalogApi(),
        NavigationScreen.PlayerRoster => PlayerRosterApi(),
        NavigationScreen.CardTemplates => new FakeCardTemplateApi(),
        NavigationScreen.Events => EventsApi(),
        _ => throw new ArgumentOutOfRangeException(nameof(screen), screen, "Add a fake API for this screen.")
    };

    private static FakeCatalogApi TalentCatalogApi()
    {
        var api = new FakeCatalogApi();
        api.AddSkill("First aid", api.AddTalent("Medicine"));
        return api;
    }

    private static FakeNetworkApi NetworkCatalogApi()
    {
        var api = new FakeNetworkApi();
        api.AddNetwork("CIV", "Civilian net", isDefault: true);
        return api;
    }

    private static FakePlayerApi PlayerRosterApi()
    {
        var api = new FakePlayerApi();
        api.AddCharacter(api.AddPlayer("Ann"), "Kira");
        return api;
    }

    private static FakeLarpEventApi EventsApi()
    {
        var api = new FakeLarpEventApi();
        var fort = api.AddEvent("Fort", true, new DateOnly(2026, 12, 9), new DateOnly(2026, 12, 10));
        fort.Registrations[FakeLarpEventApi.KiraId] = [.. fort.Days.Select(day => day.Id)];
        fort.ExperienceHistory[FakeLarpEventApi.KiraId] = [.. Enumerable.Repeat(1, 30)];
        return api;
    }

    private static void SelectFirstEntryOfEveryList(Control view)
    {
        foreach (var list in view.GetVisualDescendants().OfType<ListBox>().Where(list => list.ItemCount > 0))
        {
            list.SelectedIndex = 0;
            Dispatcher.UIThread.RunJobs();
        }
    }

    private static void Settle(Window window)
    {
        for (var pass = 0; pass < 3; pass++)
        {
            Dispatcher.UIThread.RunJobs();
            window.UpdateLayout();
        }
    }

    private static UserControl ScreenView(MainWindow window) =>
        window.GetVisualDescendants().OfType<UserControl>().Single(view => view.IsEffectivelyVisible);

    /// <summary>Rows of a list may lie outside the list: the list scrolls them into view.</summary>
    private static bool IsScrolledContent(Control view, Control control) =>
        control.GetVisualAncestors().TakeWhile(ancestor => !ReferenceEquals(ancestor, view)).OfType<ScrollContentPresenter>().Any();

    /// <summary>A text field scrolls its own text, so its placeholder and text may be wider than the field.</summary>
    private static bool IsInsideTextField(Control control) => control.GetVisualAncestors().OfType<TextBox>().Any();

    private static bool SticksOutOfItsParent(Control control)
    {
        if (control.GetVisualParent() is not Control parent)
        {
            return false;
        }

        var bounds = new Rect(control.TranslatePoint(default, parent)!.Value, control.Bounds.Size);
        return bounds.X < -Tolerance
            || bounds.Y < -Tolerance
            || bounds.Right > parent.Bounds.Width + Tolerance
            || bounds.Bottom > parent.Bounds.Height + Tolerance;
    }

    /// <summary>Text on one line that is wider than its block, so the end of it is cut off without an ellipsis.</summary>
    private static bool IsCutOffText(Control control) =>
        control is TextBlock { TextWrapping: Avalonia.Media.TextWrapping.NoWrap, TextTrimming: var trimming } block
        && trimming == Avalonia.Media.TextTrimming.None
        && block.TextLayout.WidthIncludingTrailingWhitespace > block.Bounds.Width - block.Padding.Left - block.Padding.Right + Tolerance;

    /// <summary>
    /// A line of text placed partly outside its block, so the block's clip hides it: for example text centred in a
    /// wider width than the block was finally given.
    /// </summary>
    private static bool DrawsTextOutsideItself(Control control) =>
        control is TextBlock block
        && block.TextLayout.TextLines.Any(line =>
            line.Start < -Tolerance
            || line.Start + line.Width > block.Bounds.Width - block.Padding.Left - block.Padding.Right + Tolerance);

    private static string Describe(Control control)
    {
        var parent = (Control)control.GetVisualParent()!;
        var bounds = new Rect(control.TranslatePoint(default, parent)!.Value, control.Bounds.Size);
        return $"{control.GetType().Name} '{Text(control)}' at {bounds} in {parent.GetType().Name} {parent.Bounds.Size}";
    }

    private static string? Text(Control control) => control switch
    {
        TextBlock block => block.Text,
        ContentControl content => content.Content?.ToString(),
        _ => control.Name
    };

    private sealed class NullSettingsStore : IManagementSettingsStore
    {
        public ManagementSettings Load() => new();

        public void Save(ManagementSettings settings)
        {
        }
    }
}
