using SciFiLarpNetwork.Management.Tests.ViewModels.News;
using System.Diagnostics;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Headless;
using Avalonia.Media.Imaging;
using Avalonia.Threading;
using Avalonia.VisualTree;
using SciFiLarpNetwork.Management.Localization;
using SciFiLarpNetwork.Management.Settings;
using SciFiLarpNetwork.Management.Tests.ViewModels.CardTemplates;
using SciFiLarpNetwork.Management.Tests.ViewModels.LarpEvents;
using SciFiLarpNetwork.Management.Tests.ViewModels.Messages;
using SciFiLarpNetwork.Management.Tests.ViewModels.Networks;
using SciFiLarpNetwork.Management.Tests.ViewModels.Players;
using SciFiLarpNetwork.Management.Tests.ViewModels.Skills;
using SciFiLarpNetwork.Management.ViewModels;
using SciFiLarpNetwork.Management.Views;
using SciFiLarpNetwork.Management.Views.Hud;

namespace SciFiLarpNetwork.Management.Tests.Views;

/// <summary>
/// The animated sign-in panel, navigation list and workspace of the main window. Time only moves when a test ticks
/// the headless render timer and real time has passed, so each test can look at the start or the end of an animation.
/// </summary>
public sealed class MainWindowAnimationTests
{
    private static readonly string[] SignInSlot = ["SignInReveal", "SignOutReveal"];
    private static readonly TimeSpan FirstFrameDeadline = TimeSpan.FromMilliseconds(120);

    [Fact]
    public Task SigningInFoldsAwayTheCredentialFields() => AvaloniaUi.Run(async () =>
    {
        var (window, viewModel) = ShowWindow();

        await viewModel.SignInCommand.ExecuteAsync();
        LetAnimationsFinish();

        Assert.False(Named<Reveal>(window, "CredentialsReveal").Child!.IsVisible);
        window.Close();
    });

    [Fact]
    public Task SigningInShowsTheAccountName() => AvaloniaUi.Run(async () =>
    {
        var (window, viewModel) = ShowWindow();
        viewModel.UserName = "LoZeno";

        await viewModel.SignInCommand.ExecuteAsync();
        LetAnimationsFinish();

        Assert.Equal(["Administrator: LoZeno"], ShownTexts(Named<Reveal>(window, "AccountReveal")));
        window.Close();
    });

    [Fact]
    public Task SigningInSwapsTheSignInButtonForSignOut() => AvaloniaUi.Run(async () =>
    {
        var (window, viewModel) = ShowWindow();

        await viewModel.SignInCommand.ExecuteAsync();
        LetAnimationsFinish();

        Assert.Equal(["Sign out"], ShownButtons(window));
        window.Close();
    });

    [Fact]
    public Task ConnectIsHiddenBeforeSigningIn() => AvaloniaUi.Run(() =>
    {
        var (window, _) = ShowWindow();

        Assert.False(Named<Reveal>(window, "ConnectReveal").Child!.IsVisible);
        window.Close();
    });

    [Fact]
    public Task SigningInUnfoldsConnect() => AvaloniaUi.Run(async () =>
    {
        var (window, viewModel) = ShowWindow();

        await viewModel.SignInCommand.ExecuteAsync();
        LetAnimationsFinish();

        Assert.True(Named<Reveal>(window, "ConnectReveal").Bounds.Height > 0);
        window.Close();
    });

    [Fact]
    public Task SigningOutFoldsConnectAway() => AvaloniaUi.Run(async () =>
    {
        var (window, viewModel) = await ShowSignedInWindowAsync();

        await viewModel.SignOutCommand.ExecuteAsync();
        LetAnimationsFinish();

        Assert.False(Named<Reveal>(window, "ConnectReveal").Child!.IsVisible);
        window.Close();
    });

    [Fact]
    public Task SignOutButtonWaitsWhileSignInSqueezesAway() => AvaloniaUi.Run(async () =>
    {
        var (window, viewModel) = ShowWindow();

        var sinceAction = Stopwatch.StartNew();
        await viewModel.SignInCommand.ExecuteAsync();
        ShowFirstFrame(sinceAction);

        Assert.Equal(0, Named<Reveal>(window, "SignOutReveal").Progress);
        window.Close();
    });

    [Fact]
    public Task SigningOutBringsBackTheCredentialFields() => AvaloniaUi.Run(async () =>
    {
        var (window, viewModel) = await ShowSignedInWindowAsync();

        await viewModel.SignOutCommand.ExecuteAsync();
        LetAnimationsFinish();

        Assert.Equal(
            new { Credentials = true, Account = false },
            new
            {
                Credentials = Named<Reveal>(window, "CredentialsReveal").Child!.IsVisible,
                Account = Named<Reveal>(window, "AccountReveal").Child!.IsVisible
            });
        window.Close();
    });

    [Fact]
    public Task SigningOutSwapsSignOutBackForSignIn() => AvaloniaUi.Run(async () =>
    {
        var (window, viewModel) = await ShowSignedInWindowAsync();

        await viewModel.SignOutCommand.ExecuteAsync();
        LetAnimationsFinish();

        Assert.Equal(["Sign in"], ShownButtons(window));
        window.Close();
    });

    [Fact]
    public Task ConnectingDropsTheEntriesOpen() => AvaloniaUi.Run(async () =>
    {
        var (window, viewModel) = ShowWindow();

        await viewModel.ConnectCommand.ExecuteAsync();
        LetAnimationsFinish();

        Assert.Equal(
            new { Height = true, Entries = "Card templates" },
            new { Height = Named<Reveal>(window, "NavigationReveal").Bounds.Height > 0, Entries = string.Join(", ", ShownEntries(window)) });
        window.Close();
    });

    [Fact]
    public Task ConnectingStartsWithTheEntriesClosed() => AvaloniaUi.Run(async () =>
    {
        var (window, viewModel) = ShowWindow();

        var sinceAction = Stopwatch.StartNew();
        await viewModel.ConnectCommand.ExecuteAsync();
        ShowFirstFrame(sinceAction);
        window.UpdateLayout();

        Assert.True(Named<Reveal>(window, "NavigationReveal").Bounds.Height < Named<ListBox>(window, "NavigationList").Bounds.Height / 2);
        window.Close();
    });

    [Fact]
    public Task ConnectingDropsEachEntryInFromAbove() => AvaloniaUi.Run(async () =>
    {
        var (window, viewModel) = ShowWindow();

        var sinceAction = Stopwatch.StartNew();
        await viewModel.ConnectCommand.ExecuteAsync();
        ShowFirstFrame(sinceAction);

        var entry = Named<ListBox>(window, "NavigationList").GetVisualDescendants().OfType<ListBoxItem>().First();
        Assert.True(entry.TranslatePoint(default, (Visual)entry.GetVisualParent()!)!.Value.Y < entry.Bounds.Y);
        window.Close();
    });

    [Fact]
    public Task SigningOutKeepsTheEntriesWhileTheListFoldsUp() => AvaloniaUi.Run(async () =>
    {
        var (window, viewModel) = await ShowSignedInWindowAsync();

        var sinceAction = Stopwatch.StartNew();
        await viewModel.SignOutCommand.ExecuteAsync();
        ShowFirstFrame(sinceAction);

        Assert.Equal(["Card templates"], ShownEntries(window));
        window.Close();
    });

    [Fact]
    public Task SigningOutEmptiesTheListOnceItHasFolded() => AvaloniaUi.Run(async () =>
    {
        var (window, viewModel) = await ShowSignedInWindowAsync();

        await viewModel.SignOutCommand.ExecuteAsync();
        LetAnimationsFinish();

        Assert.Empty(ShownEntries(window));
        window.Close();
    });

    [Fact]
    public Task SelectingAnEntryStartsTheWorkspaceLeftOfThePanel() => AvaloniaUi.Run(async () =>
    {
        var (window, viewModel) = await ShowSignedInWindowAsync();

        var sinceAction = Stopwatch.StartNew();
        viewModel.SelectedNavigationItem = viewModel.NavigationItems[0];
        ShowFirstFrame(sinceAction, HudAnimations.SlideOutDuration);

        Assert.True(WorkspaceOffset(window) < 0);
        window.Close();
    });

    [Fact]
    public Task SelectingAnEntryKeepsTheOldViewOnScreenWhileItLeaves() => AvaloniaUi.Run(async () =>
    {
        var (window, viewModel) = await ShowSignedInWindowAsync();

        viewModel.SelectedNavigationItem = viewModel.NavigationItems[0];
        Dispatcher.UIThread.RunJobs();

        Assert.True(Named<Image>(window, "LeavingWorkspace").IsVisible);
        window.Close();
    });

    [Fact]
    public Task TheOldViewSlidesOutToTheLeft() => AvaloniaUi.Run(async () =>
    {
        var (window, viewModel) = await ShowSignedInWindowAsync();

        viewModel.SelectedNavigationItem = viewModel.NavigationItems[0];
        ShowFrameMidwayThroughTheSlideOut();

        Assert.True(OffsetOf(Named<Image>(window, "LeavingWorkspace")) < -Named<Grid>(window, "Workspace").Bounds.Width / 4);
        window.Close();
    });

    [Fact]
    public Task TheNewViewWaitsOffThePanelWhileTheOldLeaves() => AvaloniaUi.Run(async () =>
    {
        var (window, viewModel) = await ShowSignedInWindowAsync();

        viewModel.SelectedNavigationItem = viewModel.NavigationItems[0];
        ShowFrameMidwayThroughTheSlideOut();

        Assert.Equal(-Named<Grid>(window, "Workspace").Bounds.Width, WorkspaceOffset(window));
        window.Close();
    });

    [Fact]
    public Task TheLeavingViewLooksLikeTheScreenOnAScaledScreen() => AvaloniaUi.Run(async () =>
    {
        var (window, viewModel) = await ShowSignedInWindowAsync();
        window.SetRenderScaling(1.75);
        LetAnimationsFinish();
        var screen = window.CaptureRenderedFrame()!;

        viewModel.SelectedNavigationItem = viewModel.NavigationItems[0];
        window.UpdateLayout();

        Assert.True(TextMismatch(screen, AsDrawn(Named<Image>(window, "LeavingWorkspace"), window.RenderScaling), WorkspacePixelOrigin(window)) < 0.5);
        window.Close();
    });

    [Fact]
    public Task TheOldViewIsDrawnAtTheWorkspaceSizeOnAScaledScreen() => AvaloniaUi.Run(async () =>
    {
        var (window, viewModel) = await ShowSignedInWindowAsync();
        window.SetRenderScaling(1.75);
        LetAnimationsFinish();

        viewModel.SelectedNavigationItem = viewModel.NavigationItems[0];
        Dispatcher.UIThread.RunJobs();
        window.UpdateLayout();

        Assert.Equal(Named<Grid>(window, "Workspace").Bounds.Size, Named<Image>(window, "LeavingWorkspace").Bounds.Size);
        window.Close();
    });

    [Fact]
    public Task ASecondSelectionDuringTheSlideOutKeepsItsOwnLeavingView() => AvaloniaUi.Run(async () =>
    {
        var (window, viewModel) = await ShowWindowWithTwoEntriesAsync();
        viewModel.SelectedNavigationItem = viewModel.NavigationItems[0];
        ShowOneFrame();

        var sinceSecond = Stopwatch.StartNew();
        viewModel.SelectedNavigationItem = viewModel.NavigationItems[1];
        ShowOneFrame();
        ShowOneFrame();
        Assert.SkipWhen(sinceSecond.Elapsed > HudAnimations.SlideOutDuration - TimeSpan.FromMilliseconds(50),
            "The runner was too slow to see the second leaving view before it slid away.");

        Assert.True(Named<Image>(window, "LeavingWorkspace").IsVisible);
        window.Close();
    });

    [Fact]
    public Task ASecondSelectionDuringTheSlideOutEndsWithTheNewViewInPlace() => AvaloniaUi.Run(async () =>
    {
        var (window, viewModel) = await ShowWindowWithTwoEntriesAsync();
        viewModel.SelectedNavigationItem = viewModel.NavigationItems[0];
        ShowOneFrame();

        viewModel.SelectedNavigationItem = viewModel.NavigationItems[1];
        LetAnimationsFinish();

        Assert.Equal(
            new { LeavingShown = false, Offset = 0.0 },
            new { LeavingShown = Named<Image>(window, "LeavingWorkspace").IsVisible, Offset = WorkspaceOffset(window) });
        window.Close();
    });

    [Fact]
    public Task TheOldViewIsGoneOnceItHasLeft() => AvaloniaUi.Run(async () =>
    {
        var (window, viewModel) = await ShowSignedInWindowAsync();

        viewModel.SelectedNavigationItem = viewModel.NavigationItems[0];
        LetAnimationsFinish();

        Assert.False(Named<Image>(window, "LeavingWorkspace").IsVisible);
        window.Close();
    });

    [Fact]
    public Task SelectingAnEntrySlidesTheWorkspaceIntoPlace() => AvaloniaUi.Run(async () =>
    {
        var (window, viewModel) = await ShowSignedInWindowAsync();

        viewModel.SelectedNavigationItem = viewModel.NavigationItems[0];
        LetAnimationsFinish();

        Assert.Equal(0, WorkspaceOffset(window));
        window.Close();
    });

    private static T Named<T>(Window window, string name)
        where T : Control =>
        window.FindControl<T>(name)!;

    private static string[] ShownTexts(Visual root) =>
        [.. root.GetVisualDescendants().OfType<TextBlock>().Where(text => text.IsEffectivelyVisible).Select(text => text.Text ?? "")];

    private static string[] ShownButtons(Window window) =>
        [.. SignInSlot
            .Select(name => Named<Reveal>(window, name))
            .Where(reveal => reveal.Progress > 0 && reveal.Child!.IsVisible)
            .Select(reveal => (string)((Button)reveal.Child!).Content!)];

    private static string[] ShownEntries(Window window) =>
        [.. Named<ListBox>(window, "NavigationList").Items.Cast<NavigationItem>().Select(item => item.Title)];

    /// <summary>Renders the leaving view the way the screen draws it, at the screen's scaling.</summary>
    private static RenderTargetBitmap AsDrawn(Image leaving, double scaling)
    {
        var drawn = new RenderTargetBitmap(
            new PixelSize((int)Math.Ceiling(leaving.Bounds.Width * scaling), (int)Math.Ceiling(leaving.Bounds.Height * scaling)),
            new Vector(96 * scaling, 96 * scaling));
        drawn.Render(leaving);
        return drawn;
    }

    private static PixelPoint WorkspacePixelOrigin(Window window)
    {
        var origin = ((Visual)Named<Grid>(window, "Workspace").GetVisualParent()!).TranslatePoint(default, window)!.Value * window.RenderScaling;
        return new PixelPoint((int)Math.Round(origin.X), (int)Math.Round(origin.Y));
    }

    /// <summary>
    /// How much of the bright (text and line) pixels differ between the snapshot and the screen under it: 0 when they
    /// line up, about 0.25 for the 96 DPI snapshot (its text is softer), and near 1 when it is drawn at another scale.
    /// </summary>
    private static double TextMismatch(Bitmap screen, Bitmap snapshot, PixelPoint origin)
    {
        var area = new PixelRect(origin, new PixelSize(
            Math.Min(snapshot.PixelSize.Width, screen.PixelSize.Width - origin.X),
            Math.Min(snapshot.PixelSize.Height, screen.PixelSize.Height - origin.Y)));
        var onScreen = BrightPixels(screen, area);
        var inSnapshot = BrightPixels(snapshot, new PixelRect(default, area.Size));
        var either = onScreen.Zip(inSnapshot).Count(pair => pair.First || pair.Second);
        return either == 0 ? 0 : (double)onScreen.Zip(inSnapshot).Count(pair => pair.First != pair.Second) / either;
    }

    private static bool[] BrightPixels(Bitmap bitmap, PixelRect area)
    {
        var pixels = new byte[area.Width * area.Height * 4];
        var pinned = System.Runtime.InteropServices.GCHandle.Alloc(pixels, System.Runtime.InteropServices.GCHandleType.Pinned);
        try
        {
            bitmap.CopyPixels(area, pinned.AddrOfPinnedObject(), pixels.Length, area.Width * 4);
        }
        finally
        {
            pinned.Free();
        }

        return [.. Enumerable.Range(0, pixels.Length / 4).Select(pixel => pixels.Skip(pixel * 4).Take(3).Max() > 140)];
    }

    private static double WorkspaceOffset(Window window) => OffsetOf(Named<Grid>(window, "Workspace"));

    private static double OffsetOf(Visual visual) => visual.TranslatePoint(default, (Visual)visual.GetVisualParent()!)!.Value.X;

    /// <remarks>
    /// Shows frames for a third of the 200 ms slide-out, timed from its start (after the snapshot of the old view is
    /// taken), and skips the test when the runner was too slow to show one before the slide-out ended.
    /// </remarks>
    private static void ShowFrameMidwayThroughTheSlideOut()
    {
        var sinceAction = Stopwatch.StartNew();
        while (sinceAction.Elapsed < HudAnimations.SlideOutDuration / 3)
        {
            ShowOneFrame();
            Thread.Sleep(10);
        }

        ShowOneFrame();
        Assert.SkipWhen(sinceAction.Elapsed > HudAnimations.SlideOutDuration - TimeSpan.FromMilliseconds(30),
            "The runner was too slow to see the old view while it slides out.");
    }

    private static void ShowOneFrame()
    {
        Dispatcher.UIThread.RunJobs();
        AvaloniaHeadlessPlatform.ForceRenderTimerTick();
        Dispatcher.UIThread.RunJobs();
    }

    /// <remarks>
    /// The headless animation clock follows the wall clock and Avalonia 12 offers tests no clock of their own, so a
    /// stalled runner could finish the shortest animation (220 ms) before this frame. The test is then skipped,
    /// because it can no longer see the animation's start, rather than failed.
    /// </remarks>
    private static void ShowFirstFrame(Stopwatch sinceAction, TimeSpan? deadline = null)
    {
        ShowOneFrame();
        Assert.SkipWhen(sinceAction.Elapsed > (deadline ?? FirstFrameDeadline), "The runner was too slow to see the animation's first frame.");
    }

    /// <remarks>The headless animation clock follows the wall clock, so this waits for real time to pass.</remarks>
    private static void LetAnimationsFinish()
    {
        var waited = Stopwatch.StartNew();
        while (waited.Elapsed < Reveal.StepDuration * 2 + TimeSpan.FromMilliseconds(250))
        {
            ShowOneFrame();
            Thread.Sleep(10);
        }

        ShowOneFrame();
    }

    private static async Task<(MainWindow Window, MainWindowViewModel ViewModel)> ShowWindowWithTwoEntriesAsync()
    {
        var (window, viewModel) = await ShowSignedInWindowAsync();
        viewModel.NavigationItems.Add(new NavigationItem("Character card template", FakeCardTemplateApi.TemplateHref));
        LetAnimationsFinish();
        return (window, viewModel);
    }

    private static async Task<(MainWindow Window, MainWindowViewModel ViewModel)> ShowSignedInWindowAsync()
    {
        var (window, viewModel) = ShowWindow();
        await viewModel.SignInCommand.ExecuteAsync();
        await viewModel.ConnectCommand.ExecuteAsync();
        LetAnimationsFinish();
        return (window, viewModel);
    }

    private static (MainWindow Window, MainWindowViewModel ViewModel) ShowWindow()
    {
        var viewModel = new MainWindowViewModel(
            new FakeCardTemplateApi(),
            new NullSettingsStore(),
            new JsonLocalizer(),
            new FakeCatalogDialogs(),
            new FakeNetworkDialogs(),
            new FakePlayerDialogs(),
            new FakeCardTemplateDialogs(),
            new FakeLarpEventDialogs(),
            new FakeMessageDialogs(), new FakeNewsDialogs(), new SciFiLarpNetwork.Management.Tests.ViewModels.Items.FakeItemDialogs(),
            new ManagementSettings());
        var window = new MainWindow(viewModel);
        window.Show();
        Dispatcher.UIThread.RunJobs();
        window.UpdateLayout();
        return (window, viewModel);
    }

    private sealed class NullSettingsStore : IManagementSettingsStore
    {
        public ManagementSettings Load() => new();

        public void Save(ManagementSettings settings)
        {
        }
    }
}
