using System.Net;
using System.Text.Json;
using SciFiLarpNetwork.Management.Api;
using SciFiLarpNetwork.Management.Localization;
using SciFiLarpNetwork.Management.Tests.ViewModels.Skills;
using SciFiLarpNetwork.Management.ViewModels.Players;

namespace SciFiLarpNetwork.Management.Tests.ViewModels.Players;

public sealed class CharacterCardPrintingTests
{
    [Fact]
    public async Task PrintingNeedsASelectedCharacter()
    {
        var roster = await RosterWithCharactersAsync(new FakePlayerApi(), new FakePlayerDialogs());

        Assert.False(roster.PreviewCardsCommand.CanExecute(null));
    }

    [Fact]
    public async Task ReaderCanPrintSelectedCharacters()
    {
        var roster = await RosterWithCharactersAsync(new FakePlayerApi { CanWrite = false }, new FakePlayerDialogs());

        roster.SelectedCharacters.Add(roster.Characters[0]);

        Assert.True(roster.ExportCardsCommand.CanExecute(null));
    }

    [Fact]
    public async Task PrintSendsTheSelectedCharactersInListOrder()
    {
        var api = new FakePlayerApi();
        var roster = await RosterWithCharactersAsync(api, new FakePlayerDialogs());
        roster.SelectedCharacters.Add(roster.Characters[1]);
        roster.SelectedCharacters.Add(roster.Characters[0]);

        await roster.PreviewCardsCommand.ExecuteAsync();

        Assert.Equal(
            new Submission("print-character-cards", JsonSerializer.Serialize(
                new { characterIds = new[] { roster.Characters[0].CharacterId, roster.Characters[1].CharacterId } })),
            api.Submissions[^1]);
    }

    [Fact]
    public async Task PreviewOpensThePrintedPdf()
    {
        var dialogs = new FakePlayerDialogs();
        var roster = await RosterWithCharactersAsync(new FakePlayerApi(), dialogs);
        roster.SelectedCharacters.Add(roster.Characters[0]);

        await roster.PreviewCardsCommand.ExecuteAsync();

        Assert.Same(FakePlayerApi.PrintedPdf, dialogs.LastPreviewedPdf);
    }

    [Fact]
    public async Task PreviewWithoutAViewerSaysSo()
    {
        var roster = await RosterWithCharactersAsync(new FakePlayerApi(), new FakePlayerDialogs { PreviewResult = false });
        roster.SelectedCharacters.Add(roster.Characters[0]);

        await roster.PreviewCardsCommand.ExecuteAsync();

        Assert.Equal(new JsonLocalizer().Translate("players.cardsPreviewFailed", "en"), roster.StatusMessage);
    }

    [Fact]
    public async Task ExportSavesThePrintedPdf()
    {
        var dialogs = new FakePlayerDialogs();
        var roster = await RosterWithCharactersAsync(new FakePlayerApi(), dialogs);
        roster.SelectedCharacters.Add(roster.Characters[0]);

        await roster.ExportCardsCommand.ExecuteAsync();

        Assert.Same(FakePlayerApi.PrintedPdf, dialogs.LastExportedPdf);
    }

    [Fact]
    public async Task ExportReportsTheSavedFile()
    {
        var roster = await RosterWithCharactersAsync(new FakePlayerApi(), new FakePlayerDialogs());
        roster.SelectedCharacters.Add(roster.Characters[0]);

        await roster.ExportCardsCommand.ExecuteAsync();

        Assert.Equal(new JsonLocalizer().Translate("players.cardsExported", "en"), roster.StatusMessage);
    }

    [Fact]
    public async Task CancelledExportReportsNothing()
    {
        var roster = await RosterWithCharactersAsync(new FakePlayerApi(), new FakePlayerDialogs { ExportResult = false });
        roster.SelectedCharacters.Add(roster.Characters[0]);

        await roster.ExportCardsCommand.ExecuteAsync();

        Assert.Null(roster.StatusMessage);
    }

    [Fact]
    public async Task PrintErrorIsShownLocalized()
    {
        var api = new FakePlayerApi();
        var roster = await RosterWithCharactersAsync(api, new FakePlayerDialogs());
        roster.SelectedCharacters.Add(roster.Characters[0]);
        api.SubmitFailures["print-character-cards"] =
            new ManagementApiException("errors.cardRenderingFailed", HttpStatusCode.InternalServerError);

        await roster.ExportCardsCommand.ExecuteAsync();

        Assert.Equal(new JsonLocalizer().Translate("errors.cardRenderingFailed", "en"), roster.StatusMessage);
    }

    [Fact]
    public async Task ReloadingTheListDropsSelectionsNoLongerShown()
    {
        var api = new FakePlayerApi();
        var roster = await RosterWithCharactersAsync(api, new FakePlayerDialogs());
        roster.SelectedCharacters.Add(roster.Characters[0]);

        roster.SelectedPlayer = roster.Players[1];

        Assert.Empty(roster.SelectedCharacters);
    }

    [Fact]
    public async Task ReloadingTheListSelectsTheCurrentCharacterForPrinting()
    {
        var roster = await RosterWithCharactersAsync(new FakePlayerApi(), new FakePlayerDialogs());
        roster.SelectedCharacter = roster.Characters[0];

        roster.ShowArchivedCharacters = true;

        Assert.Equal([roster.Characters[0]], roster.SelectedCharacters);
    }

    [Fact]
    public async Task ReloadingTheListKeepsOnlyTheCurrentCharacterSelected()
    {
        var roster = await RosterWithCharactersAsync(new FakePlayerApi(), new FakePlayerDialogs());
        roster.SelectedCharacter = roster.Characters[1];
        roster.SelectedCharacters.Add(roster.Characters[0]);
        roster.SelectedCharacters.Add(roster.Characters[1]);

        roster.ShowArchivedCharacters = true;

        Assert.Equal([roster.Characters[1]], roster.SelectedCharacters);
    }

    [Fact]
    public async Task SingleCharacterCommandsAreOffWhileSeveralAreSelected()
    {
        var roster = await RosterWithCharactersAsync(new FakePlayerApi(), new FakePlayerDialogs());
        roster.SelectedCharacter = roster.Characters[0];
        roster.SelectedCharacters.Add(roster.Characters[0]);
        roster.SelectedCharacters.Add(roster.Characters[1]);

        Assert.Equal(
            [false, false, false],
            [
                roster.EditCharacterCommand.CanExecute(null),
                roster.ToggleCharacterArchivedCommand.CanExecute(null),
                roster.RegenerateQrCommand.CanExecute(null)
            ]);
    }

    [Fact]
    public async Task SingleCharacterCommandsWorkWithOneSelected()
    {
        var roster = await RosterWithCharactersAsync(new FakePlayerApi(), new FakePlayerDialogs());
        roster.SelectedCharacter = roster.Characters[0];
        roster.SelectedCharacters.Add(roster.Characters[0]);

        Assert.True(roster.EditCharacterCommand.CanExecute(null));
    }

    private static async Task<PlayerRosterViewModel> RosterWithCharactersAsync(FakePlayerApi api, FakePlayerDialogs dialogs)
    {
        var ann = api.AddPlayer("Ann");
        api.AddPlayer("Bob");
        api.AddCharacter(ann, "Kira");
        api.AddCharacter(ann, "Vel");
        var roster = new PlayerRosterViewModel(api, new JsonLocalizer(), dialogs, "http://localhost/api", "en", FakePlayerApi.PlayersHref);
        await roster.LoadAsync();
        roster.SelectedPlayer = roster.Players[0];
        return roster;
    }
}
