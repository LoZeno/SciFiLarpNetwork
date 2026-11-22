using SciFiLarpNetwork.Management.Localization;
using SciFiLarpNetwork.Management.Tests.ViewModels.LarpEvents;
using SciFiLarpNetwork.Management.Tests.ViewModels.Networks;
using SciFiLarpNetwork.Management.Tests.ViewModels.Players;
using SciFiLarpNetwork.Management.Tests.ViewModels.Skills;
using SciFiLarpNetwork.Management.ViewModels.LarpEvents;
using SciFiLarpNetwork.Management.ViewModels.Networks;
using SciFiLarpNetwork.Management.ViewModels.Players;
using SciFiLarpNetwork.Management.ViewModels.Skills;

namespace SciFiLarpNetwork.Management.Tests.ViewModels;

/// <summary>
/// Italian adjectives agree with the noun they describe: giocatore, personaggio and evento are masculine ("Nuovo",
/// "archiviato"); rete, attitudine, abilità and statistica are feminine ("Nuova", "archiviata").
/// </summary>
public sealed class ItalianNounAgreementTests
{
    private const string ApiBaseUrl = "http://localhost/api";

    [Fact]
    public async Task PlayerLabelsAreMasculine()
    {
        var api = new FakePlayerApi();
        api.AddPlayer("Ann").IsArchived = true;
        var roster = new PlayerRosterViewModel(api, new JsonLocalizer(), new FakePlayerDialogs(), ApiBaseUrl, "it", FakePlayerApi.PlayersHref);
        await roster.LoadAsync();

        roster.ShowArchivedPlayers = true;

        Assert.Equal(
            new { New = "Nuovo", ShowArchived = "Mostra archiviati", Player = "#1 Ann (Archiviato)" },
            new { New = roster.NewPlayerLabel, ShowArchived = roster.ShowArchivedPlayersLabel, Player = roster.Players.Single().DisplayName });
    }

    [Fact]
    public async Task CharacterLabelsAreMasculine()
    {
        var api = new FakePlayerApi();
        api.AddCharacter(api.AddPlayer("Ann"), "Kira").IsArchived = true;
        var roster = new PlayerRosterViewModel(api, new JsonLocalizer(), new FakePlayerDialogs(), ApiBaseUrl, "it", FakePlayerApi.PlayersHref);
        await roster.LoadAsync();
        roster.ShowArchivedCharacters = true;

        roster.SelectedPlayer = roster.Players[0];

        Assert.Equal(
            new { New = "Nuovo", ShowArchived = "Mostra archiviati", Archived = true },
            new
            {
                New = roster.NewCharacterLabel,
                ShowArchived = roster.ShowArchivedCharactersLabel,
                Archived = roster.Characters.Single().DisplayName.EndsWith("(Archiviato)", StringComparison.Ordinal)
            });
    }

    [Fact]
    public async Task EventLabelsAreMasculine()
    {
        var api = new FakeLarpEventApi();
        api.AddEvent("Fort", isOpen: false).IsArchived = true;
        var screen = new LarpEventScreenViewModel(api, new JsonLocalizer(), new FakeLarpEventDialogs(), ApiBaseUrl, "it",
            FakeLarpEventApi.EventsHref);
        await screen.LoadAsync();

        screen.ShowArchived = true;

        Assert.Equal(
            new { New = "Nuovo", ShowArchived = "Mostra archiviati", Archived = true },
            new
            {
                New = screen.NewLabel,
                ShowArchived = screen.ShowArchivedLabel,
                Archived = screen.Events.Single().DisplayName.EndsWith("(Archiviato)", StringComparison.Ordinal)
            });
    }

    [Fact]
    public void NetworkNewLabelIsFeminine()
    {
        var catalog = new NetworkCatalogViewModel(new FakeNetworkApi(), new JsonLocalizer(), new FakeNetworkDialogs(), ApiBaseUrl, "it",
            FakeNetworkApi.NetworksHref);

        Assert.Equal("Nuova", catalog.NewLabel);
    }

    [Fact]
    public void TalentAndSkillLabelsAreFeminine()
    {
        var catalog = new TalentCatalogViewModel(new FakeCatalogApi(), new JsonLocalizer(), new FakeCatalogDialogs(), ApiBaseUrl, "it",
            FakeCatalogApi.TalentsHref);

        Assert.Equal(
            new { New = "Nuova", ShowArchived = "Mostra archiviate" },
            new { New = catalog.NewLabel, ShowArchived = catalog.ShowArchivedLabel });
    }

    [Fact]
    public async Task StatLabelsAreFeminine()
    {
        var api = new FakePlayerApi();
        api.AddStat("Rank", "number", isArchived: true);
        CharacterStatsViewModel? stats = null;
        var dialogs = new FakePlayerDialogs { OnStats = shown => Task.FromResult(stats = shown) };
        var roster = new PlayerRosterViewModel(api, new JsonLocalizer(), dialogs, ApiBaseUrl, "it", FakePlayerApi.PlayersHref);
        await roster.LoadAsync();
        await roster.ManageStatsCommand.ExecuteAsync();

        stats!.ShowArchived = true;

        Assert.Equal(
            new { ShowArchived = "Mostra archiviate", Archived = true },
            new { ShowArchived = stats.ShowArchivedLabel, Archived = stats.Stats.Single().DisplayName.EndsWith("(Archiviata)", StringComparison.Ordinal) });
    }
}
