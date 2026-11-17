using System.Net;
using System.Text.Json;
using SciFiLarpNetwork.Management.Api;
using SciFiLarpNetwork.Management.Localization;
using SciFiLarpNetwork.Management.Tests.ViewModels.Skills;
using SciFiLarpNetwork.Management.ViewModels.Players;

namespace SciFiLarpNetwork.Management.Tests.ViewModels.Players;

public sealed class PlayerRosterViewModelTests
{
    [Fact]
    public async Task LoadListsPlayersWithNumbersAndTags()
    {
        var api = new FakePlayerApi();
        api.AddPlayer("Ann");
        api.AddPlayer("Bob", isActive: false);
        var roster = CreateRoster(api);

        await roster.LoadAsync();

        Assert.Equal(["#1 Ann", "#2 Bob (Inactive)"], roster.Players.Select(player => player.DisplayName));
    }

    [Fact]
    public async Task ReaderGetsNoChangeCommands()
    {
        var api = new FakePlayerApi { CanWrite = false };
        api.AddPlayer("Ann");
        var roster = CreateRoster(api);

        await roster.LoadAsync();

        Assert.False(roster.NewPlayerCommand.CanExecute(null));
    }

    [Fact]
    public async Task SelectingAPlayerListsTheirCharacters()
    {
        var api = new FakePlayerApi();
        var ann = api.AddPlayer("Ann");
        api.AddCharacter(ann, "Kira");
        api.AddCharacter(ann, "Vel", status: "dead");
        var roster = await LoadedRosterAsync(api);

        roster.SelectedPlayer = roster.Players[0];

        Assert.Equal(["#1 Kira (Active)", "#2 Vel (Dead)"], roster.Characters.Select(character => character.DisplayName));
    }

    [Fact]
    public async Task SearchSendsTheTextAsAQueryParameter()
    {
        var api = new FakePlayerApi();
        api.AddPlayer("Ann");
        api.AddPlayer("Bob");
        var roster = await LoadedRosterAsync(api);

        roster.PlayerSearch = "bo b";
        await roster.SearchPlayersCommand.ExecuteAsync();

        Assert.Contains("/api/v1/players?search=bo%20b", api.Gets);
    }

    [Fact]
    public async Task SearchShowsOnlyMatchingPlayers()
    {
        var api = new FakePlayerApi();
        api.AddPlayer("Ann");
        api.AddPlayer("Bob");
        var roster = await LoadedRosterAsync(api);

        roster.PlayerSearch = "bo";
        await roster.SearchPlayersCommand.ExecuteAsync();

        Assert.Equal(["Bob"], roster.Players.Select(player => player.Name));
    }

    [Fact]
    public async Task NextPageFollowsTheNextLink()
    {
        var api = new FakePlayerApi { PageSize = 1 };
        api.AddPlayer("Ann");
        api.AddPlayer("Bob");
        var roster = await LoadedRosterAsync(api);

        await roster.NextPlayersCommand.ExecuteAsync();

        Assert.Equal(new { Names = "Bob", Page = "Page 2 of 2 (2 in total)" },
            new { Names = string.Join(", ", roster.Players.Select(player => player.Name)), Page = roster.PlayerPage });
    }

    [Fact]
    public async Task NextPageDoesNotReloadTheFirstPage()
    {
        var api = new FakePlayerApi { PageSize = 1 };
        api.AddPlayer("Ann");
        api.AddPlayer("Bob");
        var roster = await LoadedRosterAsync(api);
        api.Gets.Clear();

        await roster.NextPlayersCommand.ExecuteAsync();

        Assert.DoesNotContain(FakePlayerApi.PlayersHref, api.Gets);
    }

    [Fact]
    public async Task NewPlayerSubmitsTheDialogValues()
    {
        var api = new FakePlayerApi();
        var dialogs = new FakePlayerDialogs
        {
            OnPlayerEditor = async editor =>
            {
                editor.Name = "Ann";
                editor.Email = "ann@example.org";
                editor.SelectedType = editor.Types[1];
                await editor.SaveCommand.ExecuteAsync();
            }
        };
        var roster = await LoadedRosterAsync(api, dialogs);

        await roster.NewPlayerCommand.ExecuteAsync();

        Assert.Equal(
            new Submission("create-player", """{"displayName":"Ann","email":"ann@example.org","type":"gameMaster"}"""),
            api.Submissions.Single());
    }

    [Fact]
    public async Task DeactivationAsksFirstAndRetiresTheCharacters()
    {
        var api = new FakePlayerApi();
        var ann = api.AddPlayer("Ann");
        api.AddCharacter(ann, "Kira");
        var dialogs = new FakePlayerDialogs();
        var roster = await LoadedRosterAsync(api, dialogs);
        roster.SelectedPlayer = roster.Players[0];

        await roster.TogglePlayerActiveCommand.ExecuteAsync();

        Assert.Equal(
            new { Asked = true, Characters = "#1 Kira (Retired)" },
            new
            {
                Asked = dialogs.LastConfirmation is not null,
                Characters = string.Join(", ", roster.Characters.Select(character => character.DisplayName))
            });
    }

    [Fact]
    public async Task DeclinedConfirmationChangesNothing()
    {
        var api = new FakePlayerApi();
        api.AddPlayer("Ann");
        var roster = await LoadedRosterAsync(api, new FakePlayerDialogs { ConfirmResult = false });
        roster.SelectedPlayer = roster.Players[0];

        await roster.TogglePlayerActiveCommand.ExecuteAsync();

        Assert.Empty(api.Submissions);
    }

    [Fact]
    public async Task ActivePlayerCannotBeArchived()
    {
        var api = new FakePlayerApi();
        api.AddPlayer("Ann");
        var roster = await LoadedRosterAsync(api);

        roster.SelectedPlayer = roster.Players[0];

        Assert.False(roster.TogglePlayerArchivedCommand.CanExecute(null));
    }

    [Fact]
    public async Task CharacterSearchListsMatchesWithTheirPlayer()
    {
        var api = new FakePlayerApi();
        api.AddCharacter(api.AddPlayer("Ann"), "Kira");
        api.AddCharacter(api.AddPlayer("Bob"), "Kiran");
        var roster = await LoadedRosterAsync(api);

        roster.CharacterSearch = "kir";
        await roster.SearchCharactersCommand.ExecuteAsync();

        Assert.Equal(["#1 Kira — Ann (Active)", "#2 Kiran — Bob (Active)"], roster.Characters.Select(character => character.DisplayName));
    }

    [Fact]
    public async Task ClearingTheSearchShowsTheSelectedPlayersCharactersAgain()
    {
        var api = new FakePlayerApi();
        var ann = api.AddPlayer("Ann");
        api.AddCharacter(ann, "Kira");
        api.AddCharacter(api.AddPlayer("Bob"), "Kiran");
        var roster = await LoadedRosterAsync(api);
        roster.SelectedPlayer = roster.Players[0];
        roster.CharacterSearch = "kir";
        await roster.SearchCharactersCommand.ExecuteAsync();

        await roster.ClearCharacterSearchCommand.ExecuteAsync();

        Assert.Equal(["Kira"], roster.Characters.Select(character => character.Name));
    }

    [Fact]
    public async Task QrLookupShowsTheCharacter()
    {
        var api = new FakePlayerApi();
        var kira = api.AddCharacter(api.AddPlayer("Ann"), "Kira");
        var roster = await LoadedRosterAsync(api);

        roster.QrIdentifierSearch = kira.QrIdentifier;
        await roster.FindByQrCommand.ExecuteAsync();

        Assert.Equal(["#1 Kira — Ann (Active)"], roster.Characters.Select(character => character.DisplayName));
    }

    [Fact]
    public async Task MalformedQrIdentifierShowsTheError()
    {
        var api = new FakePlayerApi();
        var roster = await LoadedRosterAsync(api);

        roster.QrIdentifierSearch = "nope";
        await roster.FindByQrCommand.ExecuteAsync();

        Assert.Equal(new JsonLocalizer().TranslateApiError("errors.invalidQrIdentifier", "en"), roster.StatusMessage);
    }

    [Fact]
    public async Task CharacterDialogShowsTheDefaultNetworkAndTheOtherChoices()
    {
        var api = new FakePlayerApi();
        api.AddPlayer("Ann");
        api.AddNetwork("Core", isDefault: true);
        api.AddNetwork("Rim");
        api.AddNetwork("Old", isEnabled: false);
        var dialogs = new FakePlayerDialogs();
        var roster = await LoadedRosterAsync(api, dialogs);
        roster.SelectedPlayer = roster.Players[0];

        await roster.NewCharacterCommand.ExecuteAsync();

        var editor = dialogs.LastCharacterEditor!;
        Assert.Equal(
            new { Default = "Core (default network, always included)", Choices = "None, Rim" },
            new { Default = editor.DefaultNetwork, Choices = string.Join(", ", editor.Networks.Select(network => network.Title)) });
    }

    [Fact]
    public async Task CharacterDialogKeepsADisabledNetworkTheCharacterAlreadyHas()
    {
        var api = new FakePlayerApi();
        var old = api.AddNetwork("Old", isEnabled: false);
        var kira = api.AddCharacter(api.AddPlayer("Ann"), "Kira");
        kira.NetworkId = old.Id;
        var dialogs = new FakePlayerDialogs();
        var roster = await LoadedRosterAsync(api, dialogs);
        roster.SelectedPlayer = roster.Players[0];
        roster.SelectedCharacter = roster.Characters[0];

        await roster.EditCharacterCommand.ExecuteAsync();

        Assert.Equal("Old (Disabled)", dialogs.LastCharacterEditor!.SelectedNetwork!.Title);
    }

    [Fact]
    public async Task NewCharacterSubmitsStatValuesAndNewStats()
    {
        var api = new FakePlayerApi();
        api.AddPlayer("Ann");
        var rank = api.AddStat("Rank", "number");
        var dialogs = new FakePlayerDialogs
        {
            OnCharacterEditor = async editor =>
            {
                editor.Name = "Kira";
                editor.ExperiencePoints = "120";
                editor.Stats[0].Value = "3";
                editor.AddNewStatCommand.Execute(null);
                editor.NewStats[0].Name = "Teaching";
                editor.NewStats[0].SelectedType = editor.NewStats[0].Types[1];
                editor.NewStats[0].Value = "adept";
                await editor.SaveCommand.ExecuteAsync();
            }
        };
        var roster = await LoadedRosterAsync(api, dialogs);
        roster.SelectedPlayer = roster.Players[0];

        await roster.NewCharacterCommand.ExecuteAsync();

        Assert.Equal(
            new Submission("create-character",
                $$"""{"name":"Kira","title":null,"species":null,"gender":null,"type":null,"experiencePoints":120,"networkId":null,"statValues":[{"number":3,"statId":"{{rank.Id}}"}],"newStats":[{"text":"adept","name":"Teaching","type":"text"}]}"""),
            api.Submissions.Single());
    }

    [Fact]
    public async Task NewCharacterDialogStartsEachStatAtItsDefault()
    {
        var api = new FakePlayerApi();
        api.AddPlayer("Ann");
        api.AddStat("Dark Side", "number").DefaultNumber = 5;
        api.AddStat("Teaching", "text").DefaultText = "adept";
        var dialogs = new FakePlayerDialogs();
        var roster = await LoadedRosterAsync(api, dialogs);
        roster.SelectedPlayer = roster.Players[0];

        await roster.NewCharacterCommand.ExecuteAsync();

        Assert.Equal(["5", "adept"], dialogs.LastCharacterEditor!.Stats.Select(stat => stat.Value));
    }

    [Theory]
    [InlineData("-1")]
    [InlineData("lots")]
    public async Task InvalidExperiencePointsShowTheErrorAndSendNothing(string points)
    {
        var api = new FakePlayerApi();
        api.AddPlayer("Ann");
        CharacterEditorViewModel? shown = null;
        var dialogs = new FakePlayerDialogs
        {
            OnCharacterEditor = async editor =>
            {
                shown = editor;
                editor.Name = "Kira";
                editor.ExperiencePoints = points;
                await editor.SaveCommand.ExecuteAsync();
            }
        };
        var roster = await LoadedRosterAsync(api, dialogs);
        roster.SelectedPlayer = roster.Players[0];

        await roster.NewCharacterCommand.ExecuteAsync();

        Assert.Equal(
            new { Sent = 0, Error = (string?)new JsonLocalizer().TranslateApiError("errors.invalidExperiencePoints", "en") },
            new { Sent = api.Submissions.Count, Error = shown!.ErrorMessage });
    }

    [Fact]
    public async Task EditingADeadCharacterSendsItsDeathDate()
    {
        var api = new FakePlayerApi();
        api.AddCharacter(api.AddPlayer("Ann"), "Kira");
        var dialogs = new FakePlayerDialogs
        {
            OnCharacterEditor = async editor =>
            {
                editor.SelectedStatus = editor.Statuses[2];
                editor.DeathDate = new DateTimeOffset(2026, 5, 1, 0, 0, 0, TimeSpan.Zero);
                await editor.SaveCommand.ExecuteAsync();
            }
        };
        var roster = await LoadedRosterAsync(api, dialogs);
        roster.SelectedPlayer = roster.Players[0];
        roster.SelectedCharacter = roster.Characters[0];

        await roster.EditCharacterCommand.ExecuteAsync();

        var body = JsonDocument.Parse(api.Submissions.Single().Body!).RootElement;
        Assert.Equal(("dead", "2026-05-01", 1L),
            (body.GetProperty("status").GetString(), body.GetProperty("deathDate").GetString(), body.GetProperty("version").GetInt64()));
    }

    [Fact]
    public async Task StaleCharacterEditShowsTheConflictAndReloadsTheLatestValues()
    {
        var api = new FakePlayerApi();
        var kira = api.AddCharacter(api.AddPlayer("Ann"), "Kira");
        var dialogs = new FakePlayerDialogs
        {
            OnCharacterEditor = async editor =>
            {
                kira.Name = "Kira Nerys";
                kira.Version = 2;
                editor.Name = "Kira N";
                await editor.SaveCommand.ExecuteAsync();
            }
        };
        var roster = await LoadedRosterAsync(api, dialogs);
        roster.SelectedPlayer = roster.Players[0];
        roster.SelectedCharacter = roster.Characters[0];

        await roster.EditCharacterCommand.ExecuteAsync();

        var editor = dialogs.LastCharacterEditor!;
        Assert.Equal(
            new { Name = "Kira Nerys", Error = (string?)new JsonLocalizer().TranslateApiError("errors.staleVersion", "en") },
            new { editor.Name, Error = editor.ErrorMessage });
    }

    [Fact]
    public async Task ActiveCharacterCannotBeArchived()
    {
        var api = new FakePlayerApi();
        api.AddCharacter(api.AddPlayer("Ann"), "Kira");
        var roster = await LoadedRosterAsync(api);
        roster.SelectedPlayer = roster.Players[0];

        roster.SelectedCharacter = roster.Characters[0];

        Assert.False(roster.ToggleCharacterArchivedCommand.CanExecute(null));
    }

    [Fact]
    public async Task RetiredCharacterIsArchivedAfterConfirmation()
    {
        var api = new FakePlayerApi();
        api.AddCharacter(api.AddPlayer("Ann"), "Kira", status: "retired");
        var roster = await LoadedRosterAsync(api);
        roster.SelectedPlayer = roster.Players[0];
        roster.SelectedCharacter = roster.Characters[0];

        await roster.ToggleCharacterArchivedCommand.ExecuteAsync();

        Assert.Equal("archive-character", api.Submissions.Single().ActionName);
    }

    [Fact]
    public async Task NewQrIdentifierIsRequestedAfterConfirmation()
    {
        var api = new FakePlayerApi();
        api.AddCharacter(api.AddPlayer("Ann"), "Kira");
        var dialogs = new FakePlayerDialogs();
        var roster = await LoadedRosterAsync(api, dialogs);
        roster.SelectedPlayer = roster.Players[0];
        roster.SelectedCharacter = roster.Characters[0];

        await roster.RegenerateQrCommand.ExecuteAsync();

        Assert.Equal(
            new { Action = "regenerate-qr-identifier", Asked = true },
            new { Action = api.Submissions.Single().ActionName, Asked = dialogs.LastConfirmation is not null });
    }

    [Fact]
    public async Task StatsWindowRenamesTheSelectedStat()
    {
        var api = new FakePlayerApi();
        api.AddStat("Rank", "number");
        var dialogs = new FakePlayerDialogs
        {
            OnStats = async stats =>
            {
                stats.SelectedStat = stats.Stats[0];
                stats.NewName = "Grade";
                await stats.RenameCommand.ExecuteAsync();
            }
        };
        var roster = await LoadedRosterAsync(api, dialogs);

        await roster.ManageStatsCommand.ExecuteAsync();

        Assert.Equal(new Submission("rename-character-stat", """{"version":1,"name":"Grade"}"""), api.Submissions.Single());
    }

    [Fact]
    public async Task StatsWindowListsStatsWithTheirType()
    {
        var api = new FakePlayerApi();
        api.AddStat("Rank", "number");
        api.AddStat("Teaching", "text");
        CharacterStatsViewModel? shown = null;
        var dialogs = new FakePlayerDialogs { OnStats = stats => Task.FromResult(shown = stats) };
        var roster = await LoadedRosterAsync(api, dialogs);

        await roster.ManageStatsCommand.ExecuteAsync();

        Assert.Equal(["Rank (Number)", "Teaching (Text)"], shown!.Stats.Select(stat => stat.DisplayName));
    }

    [Fact]
    public async Task ExpiredSessionIsReported()
    {
        var api = new FakePlayerApi();
        api.AddPlayer("Ann");
        api.SubmitFailures["deactivate-player"] = new ManagementApiException("errors.unauthorized", HttpStatusCode.Unauthorized);
        var roster = await LoadedRosterAsync(api);
        var expired = false;
        roster.SessionExpired += (_, _) => expired = true;
        roster.SelectedPlayer = roster.Players[0];

        await roster.TogglePlayerActiveCommand.ExecuteAsync();

        Assert.True(expired);
    }

    private static PlayerRosterViewModel CreateRoster(FakePlayerApi api, FakePlayerDialogs? dialogs = null) =>
        new(api, new JsonLocalizer(), dialogs ?? new FakePlayerDialogs(), "http://localhost/api", "en", FakePlayerApi.PlayersHref);

    private static async Task<PlayerRosterViewModel> LoadedRosterAsync(FakePlayerApi api, FakePlayerDialogs? dialogs = null)
    {
        var roster = CreateRoster(api, dialogs);
        await roster.LoadAsync();
        return roster;
    }
}
