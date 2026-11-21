using System.Diagnostics;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Headless;
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
using SciFiLarpNetwork.Management.Views.Hud;

namespace SciFiLarpNetwork.Management.Tests.Views;

/// <summary>
/// The animated sign-in panel, navigation list and workspace of the main window. Time only moves when a test ticks
/// the headless render timer and real time has passed, so each test can look at the start or the end of an animation.
/// </summary>
public sealed class MainWindowAnimationTests
{
    private static readonly string[] SignInSlot = ["SignInReveal", "SignOutReveal"];

    [AvaloniaFact]
    public async Task SigningInFoldsAwayTheCredentialFields()
    {
        var (window, viewModel) = ShowWindow();

        await viewModel.SignInCommand.ExecuteAsync();
        LetAnimationsFinish();

        Assert.False(Named<Reveal>(window, "CredentialsReveal").Child!.IsVisible);
        window.Close();
    }

    [AvaloniaFact]
    public async Task SigningInShowsTheAccountName()
    {
        var (window, viewModel) = ShowWindow();
        viewModel.UserName = "LoZeno";

        await viewModel.SignInCommand.ExecuteAsync();
        LetAnimationsFinish();

        Assert.Equal(["Administrator: LoZeno"], ShownTexts(Named<Reveal>(window, "AccountReveal")));
        window.Close();
    }

    [AvaloniaFact]
    public async Task SigningInSwapsTheSignInButtonForSignOut()
    {
        var (window, viewModel) = ShowWindow();

        await viewModel.SignInCommand.ExecuteAsync();
        LetAnimationsFinish();

        Assert.Equal(["Sign out"], ShownButtons(window));
        window.Close();
    }

    [AvaloniaFact]
    public async Task SignOutButtonWaitsWhileSignInSqueezesAway()
    {
        var (window, viewModel) = ShowWindow();

        await viewModel.SignInCommand.ExecuteAsync();
        ShowOneFrame();

        Assert.Equal(0, Named<Reveal>(window, "SignOutReveal").Progress);
        window.Close();
    }

    [AvaloniaFact]
    public async Task SigningOutBringsBackTheCredentialFields()
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
    }

    [AvaloniaFact]
    public async Task SigningOutSwapsSignOutBackForSignIn()
    {
        var (window, viewModel) = await ShowSignedInWindowAsync();

        await viewModel.SignOutCommand.ExecuteAsync();
        LetAnimationsFinish();

        Assert.Equal(["Sign in"], ShownButtons(window));
        window.Close();
    }

    [AvaloniaFact]
    public async Task ConnectingDropsTheEntriesOpen()
    {
        var (window, viewModel) = ShowWindow();

        await viewModel.ConnectCommand.ExecuteAsync();
        LetAnimationsFinish();

        Assert.Equal(
            new { Height = true, Entries = "Card templates" },
            new { Height = Named<Reveal>(window, "NavigationReveal").Bounds.Height > 0, Entries = string.Join(", ", ShownEntries(window)) });
        window.Close();
    }

    [AvaloniaFact]
    public async Task ConnectingStartsWithTheEntriesClosed()
    {
        var (window, viewModel) = ShowWindow();

        await viewModel.ConnectCommand.ExecuteAsync();
        Dispatcher.UIThread.RunJobs();
        window.UpdateLayout();

        Assert.Equal(0, Named<Reveal>(window, "NavigationReveal").Bounds.Height);
        window.Close();
    }

    [AvaloniaFact]
    public async Task ConnectingDropsEachEntryInFromAbove()
    {
        var (window, viewModel) = ShowWindow();

        await viewModel.ConnectCommand.ExecuteAsync();
        ShowOneFrame();

        var entry = Named<ListBox>(window, "NavigationList").GetVisualDescendants().OfType<ListBoxItem>().First();
        Assert.True(entry.TranslatePoint(default, (Visual)entry.GetVisualParent()!)!.Value.Y < entry.Bounds.Y);
        window.Close();
    }

    [AvaloniaFact]
    public async Task SigningOutKeepsTheEntriesWhileTheListFoldsUp()
    {
        var (window, viewModel) = await ShowSignedInWindowAsync();

        await viewModel.SignOutCommand.ExecuteAsync();
        ShowOneFrame();

        Assert.Equal(["Card templates"], ShownEntries(window));
        window.Close();
    }

    [AvaloniaFact]
    public async Task SigningOutEmptiesTheListOnceItHasFolded()
    {
        var (window, viewModel) = await ShowSignedInWindowAsync();

        await viewModel.SignOutCommand.ExecuteAsync();
        LetAnimationsFinish();

        Assert.Empty(ShownEntries(window));
        window.Close();
    }

    [AvaloniaFact]
    public async Task SelectingAnEntryStartsTheWorkspaceLeftOfThePanel()
    {
        var (window, viewModel) = await ShowSignedInWindowAsync();

        viewModel.SelectedNavigationItem = viewModel.NavigationItems[0];
        ShowOneFrame();

        Assert.True(WorkspaceOffset(window) < 0);
        window.Close();
    }

    [AvaloniaFact]
    public async Task SelectingAnEntrySlidesTheWorkspaceIntoPlace()
    {
        var (window, viewModel) = await ShowSignedInWindowAsync();

        viewModel.SelectedNavigationItem = viewModel.NavigationItems[0];
        LetAnimationsFinish();

        Assert.Equal(0, WorkspaceOffset(window));
        window.Close();
    }

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

    private static double WorkspaceOffset(Window window)
    {
        var workspace = Named<Grid>(window, "Workspace");
        return workspace.TranslatePoint(default, (Visual)workspace.GetVisualParent()!)!.Value.X;
    }

    private static void ShowOneFrame()
    {
        Dispatcher.UIThread.RunJobs();
        AvaloniaHeadlessPlatform.ForceRenderTimerTick();
        Dispatcher.UIThread.RunJobs();
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
