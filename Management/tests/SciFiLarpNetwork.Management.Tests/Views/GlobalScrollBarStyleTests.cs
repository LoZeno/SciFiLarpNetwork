using Avalonia;
using Avalonia.Controls;
using Avalonia.Headless.XUnit;
using Avalonia.Layout;
using Avalonia.Threading;
using Avalonia.VisualTree;
using SciFiLarpNetwork.Management.Localization;
using SciFiLarpNetwork.Management.Settings;
using SciFiLarpNetwork.Management.Tests.ViewModels.CardTemplates;
using SciFiLarpNetwork.Management.Tests.ViewModels.LarpEvents;
using SciFiLarpNetwork.Management.Tests.ViewModels.Networks;
using SciFiLarpNetwork.Management.Tests.ViewModels.Players;
using SciFiLarpNetwork.Management.Tests.ViewModels.Skills;
using SciFiLarpNetwork.Management.ViewModels;
using SciFiLarpNetwork.Management.ViewModels.Networks;
using SciFiLarpNetwork.Management.Views;
using SciFiLarpNetwork.Management.Views.CardTemplates;
using SciFiLarpNetwork.Management.Views.Networks;
using static SciFiLarpNetwork.Management.Tests.Views.ScrollBarLayout;

namespace SciFiLarpNetwork.Management.Tests.Views;

/// <summary>
/// The app-wide scroll bar style (Styles/ScrollBars.axaml, included from App.axaml) on screens other than the
/// navigation panel: the networks list, a combo box drop-down, and a horizontal bar.
/// </summary>
public sealed class GlobalScrollBarStyleTests
{
    [AvaloniaFact]
    public async Task NetworkListScrollBarCoversNoNetwork()
    {
        var (window, scroller) = await ShowNetworkListAsync();

        Assert.Empty(ControlsUnder(window, scroller, VerticalBar(scroller)));
        window.Close();
    }

    [AvaloniaFact]
    public async Task NetworkListScrollBarKeepsAGap()
    {
        var (window, scroller) = await ShowNetworkListAsync();
        var content = BoundsIn(window, (Visual)scroller.Content!);

        Assert.True(BoundsIn(window, VerticalBar(scroller)).Left - content.Right >= 4);
        window.Close();
    }

    [AvaloniaFact]
    public async Task NetworkListScrollBarHasNoArrowButtons()
    {
        var (window, scroller) = await ShowNetworkListAsync();

        Assert.False(HasArrowButtons(VerticalBar(scroller)));
        window.Close();
    }

    [AvaloniaFact]
    public async Task NetworkListScrollBarIsThin()
    {
        var (window, scroller) = await ShowNetworkListAsync();

        Assert.Equal(new { Visible = true, Thickness = 6.0 },
            new { Visible = VerticalBar(scroller).IsEffectivelyVisible, Thickness = Thickness(VerticalBar(scroller)) });
        window.Close();
    }

    [AvaloniaFact]
    public void ComboBoxDropDownScrollBarIsThinAndBeside()
    {
        var comboBox = new ComboBox { ItemsSource = Enumerable.Range(1, 60).Select(item => $"Item {item}").ToArray() };
        var window = new Window { Width = 300, Height = 300, Content = comboBox };
        window.Show();
        comboBox.IsDropDownOpen = true;
        Dispatcher.UIThread.RunJobs();
        var popup = comboBox.GetVisualDescendants().OfType<Avalonia.Controls.Primitives.Popup>().Single();
        var scroller = ((Visual)popup.Child!).GetVisualDescendants().OfType<ScrollViewer>().First();

        Assert.Equal(new { AllowAutoHide = false, Thickness = 6.0, Arrows = false },
            new { scroller.AllowAutoHide, Thickness = Thickness(VerticalBar(scroller)), Arrows = HasArrowButtons(VerticalBar(scroller)) });
        window.Close();
    }

    [AvaloniaFact]
    public void HorizontalScrollBarIsThin()
    {
        var (window, scroller) = ShowWideContent();

        Assert.Equal(6.0, Thickness(HorizontalBar(scroller)));
        window.Close();
    }

    [AvaloniaFact]
    public void HorizontalScrollBarKeepsAGapBelowTheContent()
    {
        var (window, scroller) = ShowWideContent();
        var content = BoundsIn(window, scroller.Presenter!);

        Assert.True(BoundsIn(window, HorizontalBar(scroller)).Top - content.Bottom >= 4);
        window.Close();
    }

    [AvaloniaFact]
    public void HorizontalScrollBarHasNoArrowButtons()
    {
        var (window, scroller) = ShowWideContent();

        Assert.False(HasArrowButtons(HorizontalBar(scroller)));
        window.Close();
    }

    [AvaloniaFact]
    public async Task CardTemplatesScreenOpensInTheMainWindow()
    {
        var viewModel = new MainWindowViewModel(new FakeCardTemplateApi(), new NullSettingsStore(), new JsonLocalizer(),
            new FakeCatalogDialogs(), new FakeNetworkDialogs(), new FakePlayerDialogs(), new FakeCardTemplateDialogs(), new FakeLarpEventDialogs(),
            new ManagementSettings());
        var window = new MainWindow(viewModel) { Height = 480 };
        window.Show();
        await viewModel.ConnectCommand.ExecuteAsync();

        viewModel.SelectedNavigationItem = viewModel.NavigationItems[0];
        Dispatcher.UIThread.RunJobs();

        Assert.True(window.GetVisualDescendants().OfType<CardTemplateScreenView>().Single().IsEffectivelyVisible);
        window.Close();
    }

    private static async Task<(Window Window, ScrollViewer Scroller)> ShowNetworkListAsync()
    {
        var api = new FakeNetworkApi();
        for (var network = 1; network <= 40; network++)
        {
            api.AddNetwork($"net-{network}", $"Network {network}");
        }

        var catalog = new NetworkCatalogViewModel(api, new JsonLocalizer(), new FakeNetworkDialogs(), "http://localhost/api", "en",
            FakeNetworkApi.NetworksHref);
        await catalog.LoadAsync();
        var window = new Window { Width = 600, Height = 320, Content = new NetworkCatalogView { DataContext = catalog } };
        window.Show();
        Dispatcher.UIThread.RunJobs();
        window.UpdateLayout();
        var list = window.GetVisualDescendants().OfType<ListBox>().Single();
        return (window, list.GetVisualDescendants().OfType<ScrollViewer>().First());
    }

    private static (Window Window, ScrollViewer Scroller) ShowWideContent()
    {
        var scroller = new ScrollViewer
        {
            HorizontalScrollBarVisibility = Avalonia.Controls.Primitives.ScrollBarVisibility.Auto,
            Content = new Border { Width = 2000, Height = 100, HorizontalAlignment = HorizontalAlignment.Left }
        };
        var window = new Window { Width = 400, Height = 300, Content = scroller };
        window.Show();
        Dispatcher.UIThread.RunJobs();
        window.UpdateLayout();
        return (window, scroller);
    }

    private sealed class NullSettingsStore : IManagementSettingsStore
    {
        public ManagementSettings Load() => new();

        public void Save(ManagementSettings settings)
        {
        }
    }
}
