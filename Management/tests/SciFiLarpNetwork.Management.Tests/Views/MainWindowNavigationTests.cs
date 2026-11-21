using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.Primitives;
using Avalonia.Headless;
using Avalonia.Headless.XUnit;
using Avalonia.Input;
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
    public void PanelControlsStillFillThePanelWidth()
    {
        var (window, navigation) = ShowShortWindow();
        var panel = navigation.FindAncestorOfType<StackPanel>()!;

        Assert.Equal(panel.Bounds.Width, panel.Children.OfType<TextBox>().First().Bounds.Width);
        window.Close();
    }

    [AvaloniaFact]
    public void ShortWindowShowsTheScrollBar()
    {
        var (window, navigation) = ShowShortWindow();

        Assert.True(VerticalScrollBar(navigation).IsEffectivelyVisible);
        window.Close();
    }

    [AvaloniaFact]
    public void ScrollBarCoversNoControl()
    {
        var (window, navigation) = ShowShortWindow();
        var bar = BoundsIn(window, VerticalScrollBar(navigation));
        var panel = navigation.FindAncestorOfType<StackPanel>()!;

        var covered = panel.GetVisualDescendants().OfType<Control>()
            .Where(control => control is TemplatedControl or TextBlock && control.IsEffectivelyVisible)
            .Where(control => BoundsIn(window, control).Intersects(bar))
            .Select(control => control.GetType().Name);

        Assert.Empty(covered);
        window.Close();
    }

    [AvaloniaFact]
    public void ScrollBarKeepsAGapFromTheControls()
    {
        var (window, navigation) = ShowShortWindow();
        var panel = BoundsIn(window, navigation.FindAncestorOfType<StackPanel>()!);

        Assert.True(BoundsIn(window, VerticalScrollBar(navigation)).Left - panel.Right >= 4);
        window.Close();
    }

    [AvaloniaFact]
    public void ScrollBarHasNoArrowButtons()
    {
        var (window, navigation) = ShowShortWindow();

        var arrows = VerticalScrollBar(navigation).GetVisualDescendants().OfType<RepeatButton>()
            .Where(button => button.Name is "PART_LineUpButton" or "PART_LineDownButton" && button.IsEffectivelyVisible);

        Assert.Empty(arrows);
        window.Close();
    }

    [AvaloniaFact]
    public void ScrollBarThumbIsThinWithRoundEnds()
    {
        var (window, navigation) = ShowShortWindow();
        var thumb = VerticalScrollBar(navigation).GetVisualDescendants().OfType<Thumb>().Single();

        Assert.Equal(new { Width = 6.0, Radius = new CornerRadius(3) }, new { thumb.Bounds.Width, Radius = thumb.CornerRadius });
        window.Close();
    }

    [AvaloniaFact]
    public void DraggingTheThumbScrollsTheNavigation()
    {
        var (window, navigation) = ShowShortWindow();
        var scroller = navigation.FindAncestorOfType<ScrollViewer>()!;
        var thumb = VerticalScrollBar(navigation).GetVisualDescendants().OfType<Thumb>().Single();
        var start = thumb.TranslatePoint(new Point(3, 5), window)!.Value;

        window.MouseDown(start, MouseButton.Left);
        window.MouseMove(start + new Point(0, 60));
        window.MouseUp(start + new Point(0, 60), MouseButton.Left);
        Dispatcher.UIThread.RunJobs();

        Assert.True(scroller.Offset.Y > 0);
        window.Close();
    }

    [AvaloniaFact]
    public void MouseWheelScrollsTheNavigation()
    {
        var (window, navigation) = ShowShortWindow();
        var scroller = navigation.FindAncestorOfType<ScrollViewer>()!;
        var panel = navigation.FindAncestorOfType<StackPanel>()!;

        window.MouseWheel(panel.TranslatePoint(new Point(20, 20), window)!.Value, new Vector(0, -3));
        Dispatcher.UIThread.RunJobs();

        Assert.True(scroller.Offset.Y > 0);
        window.Close();
    }

    [AvaloniaFact]
    public void ControlsUseTheWholePanelWhenEverythingFits()
    {
        var (window, navigation) = ShowWindow(height: 2000);
        var scroller = navigation.FindAncestorOfType<ScrollViewer>()!;
        var panel = navigation.FindAncestorOfType<StackPanel>()!;

        Assert.Equal(scroller.Bounds.Width, panel.Children.OfType<TextBox>().First().Bounds.Width);
        window.Close();
    }

    private static ScrollBar VerticalScrollBar(ListBox navigation)
    {
        var scroller = navigation.FindAncestorOfType<ScrollViewer>()!;
        return scroller.GetVisualDescendants().OfType<ScrollBar>()
            .Single(bar => bar.Name == "PART_VerticalScrollBar" && ReferenceEquals(bar.TemplatedParent, scroller));
    }

    private static Rect BoundsIn(Visual root, Visual control) =>
        new(control.TranslatePoint(default, root)!.Value, control.Bounds.Size);

    private static (MainWindow Window, ListBox Navigation) ShowShortWindow() => ShowWindow(height: 480);

    private static (MainWindow Window, ListBox Navigation) ShowWindow(double height)
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

        var window = new MainWindow(viewModel) { Height = height };
        window.Show();
        Dispatcher.UIThread.RunJobs();
        window.UpdateLayout();
        var navigation = window.GetVisualDescendants().OfType<ListBox>()
            .Single(list => list.Name == "NavigationList");
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
