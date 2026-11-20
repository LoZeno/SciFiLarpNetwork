using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.Primitives;
using Avalonia.Headless.XUnit;
using Avalonia.Threading;
using Avalonia.VisualTree;
using SciFiLarpNetwork.Management.Localization;
using SciFiLarpNetwork.Management.Settings;
using SciFiLarpNetwork.Management.Tests.ViewModels.CardTemplates;
using SciFiLarpNetwork.Management.Tests.ViewModels.Networks;
using SciFiLarpNetwork.Management.Tests.ViewModels.Players;
using SciFiLarpNetwork.Management.Tests.ViewModels.Skills;
using SciFiLarpNetwork.Management.ViewModels;
using SciFiLarpNetwork.Management.Views;

namespace SciFiLarpNetwork.Management.Tests.Views;

public sealed class MainWindowNavigationTests
{
    private const int EntryCount = 20;

    [AvaloniaFact]
    public void NavigationListIsInsideAVerticalScrollArea()
    {
        var (window, navigation) = ShowShortWindow();

        Assert.Equal(ScrollBarVisibility.Auto, navigation.FindAncestorOfType<ScrollViewer>()?.VerticalScrollBarVisibility);
        window.Close();
    }

    [AvaloniaFact]
    public void ShortWindowCanScrollToTheLastEntry()
    {
        var (window, navigation) = ShowShortWindow();
        var last = navigation.GetVisualDescendants().OfType<ListBoxItem>().Last();

        last.BringIntoView();
        Dispatcher.UIThread.RunJobs();
        window.UpdateLayout();

        Assert.InRange(last.TranslatePoint(new Point(0, last.Bounds.Height), window)!.Value.Y, 0, window.ClientSize.Height);
        window.Close();
    }

    [AvaloniaFact]
    public void NavigationButtonsStillFillThePanelWidth()
    {
        var (window, navigation) = ShowShortWindow();
        var panel = (StackPanel)navigation.Parent!;

        Assert.Equal(panel.Bounds.Width, panel.Children.OfType<Button>().Last().Bounds.Width);
        window.Close();
    }

    private static (MainWindow Window, ListBox Navigation) ShowShortWindow()
    {
        var viewModel = new MainWindowViewModel(
            new FakeCardTemplateApi(),
            new NullSettingsStore(),
            new JsonLocalizer(),
            new FakeCatalogDialogs(),
            new FakeNetworkDialogs(),
            new FakePlayerDialogs(),
            new FakeCardTemplateDialogs(),
            new ManagementSettings());
        for (var entry = 1; entry <= EntryCount; entry++)
        {
            viewModel.NavigationItems.Add(new NavigationItem($"Entry {entry}", $"/api/v1/entry-{entry}"));
        }

        var window = new MainWindow(viewModel) { Height = 480 };
        window.Show();
        Dispatcher.UIThread.RunJobs();
        window.UpdateLayout();
        var navigation = window.GetVisualDescendants().OfType<ListBox>()
            .Single(list => ReferenceEquals(list.ItemsSource, viewModel.NavigationItems));
        return (window, navigation);
    }

    private sealed class NullSettingsStore : IManagementSettingsStore
    {
        public ManagementSettings Load() => new();

        public void Save(ManagementSettings settings)
        {
        }
    }
}
