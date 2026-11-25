using SciFiLarpNetwork.Management.Tests.ViewModels.News;
using SciFiLarpNetwork.Management.Tests.ViewModels.CardTemplates;
using SciFiLarpNetwork.Management.Tests.ViewModels.LarpEvents;
using Avalonia.Controls;
using Avalonia.Threading;
using Avalonia.VisualTree;
using SciFiLarpNetwork.Management.Localization;
using SciFiLarpNetwork.Management.Settings;
using SciFiLarpNetwork.Management.Tests.ViewModels.Messages;
using SciFiLarpNetwork.Management.Tests.ViewModels.Networks;
using SciFiLarpNetwork.Management.Tests.ViewModels.Players;
using SciFiLarpNetwork.Management.Tests.ViewModels.Skills;
using SciFiLarpNetwork.Management.ViewModels;
using SciFiLarpNetwork.Management.ViewModels.Players;
using SciFiLarpNetwork.Management.Views;
using SciFiLarpNetwork.Management.Views.Players;

namespace SciFiLarpNetwork.Management.Tests.Views;

public sealed class PlayerRosterWindowTests
{
    [Fact]
    public Task MainWindowShowsThePlayersScreenWhenItsEntryIsSelected() => AvaloniaUi.Run(async () =>
    {
        var api = new FakePlayerApi();
        api.AddPlayer("Ann");
        var viewModel = new MainWindowViewModel(
            api,
            new NullSettingsStore(),
            new JsonLocalizer(),
            new FakeCatalogDialogs(),
            new FakeNetworkDialogs(),
            new FakePlayerDialogs(),
            new FakeCardTemplateDialogs(),
            new FakeLarpEventDialogs(),
            new FakeMessageDialogs(), new FakeNewsDialogs(),
            new ManagementSettings());
        var window = new MainWindow(viewModel);
        window.Show();
        await viewModel.ConnectCommand.ExecuteAsync();

        viewModel.SelectedNavigationItem = viewModel.NavigationItems[0];
        Dispatcher.UIThread.RunJobs();

        Assert.True(window.GetVisualDescendants().OfType<PlayerRosterView>().Single().IsEffectivelyVisible);
        window.Close();
    });

    [Fact]
    public Task CharacterDialogHidesTheStatusForANewCharacter() => AvaloniaUi.Run(async () =>
    {
        var api = new FakePlayerApi();
        api.AddPlayer("Ann");
        api.AddStat("Rank", "number");
        CharacterEditorWindow? shown = null;
        var dialogs = new FakePlayerDialogs
        {
            OnCharacterEditor = editor =>
            {
                shown = new CharacterEditorWindow(editor);
                shown.Show();
                Dispatcher.UIThread.RunJobs();
                return Task.CompletedTask;
            }
        };
        var roster = new PlayerRosterViewModel(api, new JsonLocalizer(), dialogs, "http://localhost/api", "en", FakePlayerApi.PlayersHref);
        await roster.LoadAsync();
        roster.SelectedPlayer = roster.Players[0];

        await roster.NewCharacterCommand.ExecuteAsync();

        var statusLabel = shown!.GetVisualDescendants().OfType<TextBlock>().Single(block => block.Text == "Status");
        Assert.False(statusLabel.IsEffectivelyVisible);
        shown!.Close();
    });

    [Fact]
    public Task StatsWindowShowsTheStats() => AvaloniaUi.Run(async () =>
    {
        var api = new FakePlayerApi();
        api.AddStat("Rank", "number");
        CharacterStatsWindow? shown = null;
        var dialogs = new FakePlayerDialogs
        {
            OnStats = stats =>
            {
                shown = new CharacterStatsWindow(stats);
                shown.Show();
                Dispatcher.UIThread.RunJobs();
                return Task.CompletedTask;
            }
        };
        var roster = new PlayerRosterViewModel(api, new JsonLocalizer(), dialogs, "http://localhost/api", "en", FakePlayerApi.PlayersHref);
        await roster.LoadAsync();

        await roster.ManageStatsCommand.ExecuteAsync();

        Assert.Contains(shown!.GetVisualDescendants().OfType<TextBlock>(), block => block.Text == "Rank (Number)");
        shown!.Close();
    });

    [Fact]
    public Task SelectingSeveralCharactersInTheListSelectsThemForPrinting() => AvaloniaUi.Run(async () =>
    {
        var api = new FakePlayerApi();
        var ann = api.AddPlayer("Ann");
        api.AddCharacter(ann, "Kira");
        api.AddCharacter(ann, "Vel");
        var roster = new PlayerRosterViewModel(api, new JsonLocalizer(), new FakePlayerDialogs(), "http://localhost/api", "en",
            FakePlayerApi.PlayersHref);
        await roster.LoadAsync();
        roster.SelectedPlayer = roster.Players[0];
        var window = new Window { Content = new PlayerRosterView { DataContext = roster } };
        window.Show();
        Dispatcher.UIThread.RunJobs();
        var characterList = window.GetVisualDescendants().OfType<ListBox>()
            .Single(list => ReferenceEquals(list.ItemsSource, roster.Characters));

        characterList.SelectAll();

        Assert.Equal(roster.Characters, roster.SelectedCharacters);
        window.Close();
    });

    private sealed class NullSettingsStore : IManagementSettingsStore
    {
        public ManagementSettings Load() => new();

        public void Save(ManagementSettings settings)
        {
        }
    }
}
