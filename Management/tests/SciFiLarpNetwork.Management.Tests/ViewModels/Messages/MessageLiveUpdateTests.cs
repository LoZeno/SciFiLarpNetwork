using SciFiLarpNetwork.Management.Localization;
using SciFiLarpNetwork.Management.ViewModels.Messages;

namespace SciFiLarpNetwork.Management.Tests.ViewModels.Messages;

public sealed class MessageLiveUpdateTests
{
    private static readonly Guid Kira = FakeMessageApi.KiraId;
    private static readonly Guid Odo = FakeMessageApi.OdoId;

    [Fact]
    public async Task OpenScreenListensToTheAdvertisedLiveUpdates()
    {
        var api = new FakeMessageApi();

        await OpenAsync(api);

        Assert.Equal(FakeMessageApi.LiveHref, api.ListeningTo);
    }

    [Fact]
    public async Task AnnouncedMessageIsListed()
    {
        var api = new FakeMessageApi();
        var screen = await OpenAsync(api);
        var sent = api.AddMessage("Supply run", Kira, Odo);

        await api.AnnounceAsync(sent);

        Assert.Equal([sent.Id], screen.Messages.Select(message => message.MessageId));
    }

    [Fact]
    public async Task MessageToTheListedCharacterIsListed()
    {
        var api = new FakeMessageApi();
        var screen = await OpenAsync(api);
        await FilterAsync(screen, "Kira");
        var sent = api.AddMessage("For Kira", Odo, Kira);

        await api.AnnounceAsync(sent);

        Assert.Equal([sent.Id], screen.Messages.Select(message => message.MessageId));
    }

    [Fact]
    public async Task MessageToAnotherCharacterDoesNotReloadAFilteredList()
    {
        var api = new FakeMessageApi();
        var screen = await OpenAsync(api);
        await FilterAsync(screen, "Kira");
        var loads = api.ListLoads.Count;

        await api.AnnounceAsync(api.AddMessage("For Odo", Kira, Odo));

        Assert.Equal(loads, api.ListLoads.Count);
    }

    [Fact]
    public async Task AnnouncedMessageReloadsThePageTheUserIsOn()
    {
        var api = new FakeMessageApi();
        for (var index = 0; index < 51; index++)
        {
            api.AddMessage($"Message {index}", Kira, Odo);
        }

        var screen = await OpenAsync(api);
        await screen.NextMessagesCommand.ExecuteAsync();

        await api.AnnounceAsync(api.AddMessage("Newest", Kira, Odo));

        Assert.Equal(
            new { Loaded = $"{FakeMessageApi.MessagesHref}?page=2", Page = "Page 2 of 2 (52 in total)" },
            new { Loaded = api.ListLoads[^1], Page = screen.MessagePage });
    }

    [Fact]
    public async Task AnnouncedMessageKeepsTheOpenMessageWithoutOpeningItAgain()
    {
        var api = new FakeMessageApi();
        var first = api.AddMessage("First", Kira, Odo);
        var screen = await OpenAsync(api);
        screen.SelectedMessage = screen.Messages[0];

        await api.AnnounceAsync(api.AddMessage("Second", Odo, Kira));

        Assert.Equal(
            new { Selected = (Guid?)first.Id, Title = "First", Openings = 1 },
            new { Selected = screen.SelectedMessage?.MessageId, Title = screen.DetailTitle, Openings = api.Openings.Count });
    }

    [Fact]
    public async Task ReconnectingReloadsTheShownPage()
    {
        var api = new FakeMessageApi();
        var screen = await OpenAsync(api);
        var missed = api.AddMessage("Sent while disconnected", Kira, Odo);

        await api.ReconnectAsync();

        Assert.Equal([missed.Id], screen.Messages.Select(message => message.MessageId));
    }

    [Fact]
    public async Task ClosingTheScreenStopsListening()
    {
        var api = new FakeMessageApi();
        var screen = await OpenAsync(api);

        screen.Dispose();

        Assert.Null(api.ListeningTo);
    }

    [Fact]
    public async Task ClosedScreenIgnoresLateAnnouncements()
    {
        var api = new FakeMessageApi();
        var screen = await OpenAsync(api);
        var announce = api.AnnounceAsync;
        screen.Dispose();
        var loads = api.ListLoads.Count;

        await api.ReconnectAsync();
        await announce(api.AddMessage("Late", Kira, Odo));

        Assert.Equal(loads, api.ListLoads.Count);
    }

    [Fact]
    public async Task ConnectionLostForGoodIsExplained()
    {
        var api = new FakeMessageApi();
        var screen = await OpenAsync(api);

        await api.LoseConnectionAsync(stillRetrying: false);

        Assert.Equal("Live updates are not available, so new messages appear only when the list is loaded again.", screen.LiveUpdatesNote);
    }

    [Fact]
    public async Task ConnectionLostForGoodIsReleased()
    {
        var api = new FakeMessageApi();
        await OpenAsync(api);

        await api.LoseConnectionAsync(stillRetrying: false);

        Assert.Null(api.ListeningTo);
    }

    [Fact]
    public async Task NextLoadListensAgainAfterTheConnectionWasLostForGood()
    {
        var api = new FakeMessageApi();
        var screen = await OpenAsync(api);
        await api.LoseConnectionAsync(stillRetrying: false);

        await screen.LoadAsync();

        Assert.Equal(new { Count = 2, Href = (string?)FakeMessageApi.LiveHref }, new { Count = api.ListenCount, Href = api.ListeningTo });
    }

    [Fact]
    public async Task InterruptedConnectionIsExplainedWhileItReconnects()
    {
        var api = new FakeMessageApi();
        var screen = await OpenAsync(api);

        await api.LoseConnectionAsync(stillRetrying: true);

        Assert.Equal("Live updates are interrupted. Reconnecting…", screen.LiveUpdatesNote);
    }

    [Fact]
    public async Task ReconnectingClearsTheInterruption()
    {
        var api = new FakeMessageApi();
        var screen = await OpenAsync(api);
        await api.LoseConnectionAsync(stillRetrying: true);

        await api.ReconnectAsync();

        Assert.Null(screen.LiveUpdatesNote);
    }

    [Fact]
    public async Task ActionDuringAnInterruptionKeepsItsStatusWhenTheConnectionComesBack()
    {
        var api = new FakeMessageApi();
        var screen = await OpenAsync(api, new FakeMessageDialogs
        {
            OnMessageEditor = editor => MessageScreenViewModelTests.WriteAsync(editor, "Kira", ["Odo"])
        });
        await api.LoseConnectionAsync(stillRetrying: true);
        await screen.NewMessageCommand.ExecuteAsync();

        await api.ReconnectAsync();

        Assert.Equal("Action completed.", screen.StatusMessage);
    }

    [Fact]
    public async Task ActionDuringAnInterruptionKeepsTheInterruptionShown()
    {
        var api = new FakeMessageApi();
        var screen = await OpenAsync(api, new FakeMessageDialogs
        {
            OnMessageEditor = editor => MessageScreenViewModelTests.WriteAsync(editor, "Kira", ["Odo"])
        });
        await api.LoseConnectionAsync(stillRetrying: true);

        await screen.NewMessageCommand.ExecuteAsync();

        Assert.Equal("Live updates are interrupted. Reconnecting…", screen.LiveUpdatesNote);
    }

    [Fact]
    public async Task ListeningAgainClearsTheUnavailableNote()
    {
        var api = new FakeMessageApi();
        var screen = await OpenAsync(api);
        await api.LoseConnectionAsync(stillRetrying: false);

        await screen.LoadAsync();

        Assert.Null(screen.LiveUpdatesNote);
    }

    [Fact]
    public async Task LiveUpdatesNoteFollowsTheLanguage()
    {
        var api = new FakeMessageApi();
        var screen = await OpenAsync(api);
        await api.LoseConnectionAsync(stillRetrying: true);

        screen.Locale = "it";

        Assert.Equal("Aggiornamenti in tempo reale interrotti. Nuovo collegamento in corso…", screen.LiveUpdatesNote);
    }

    [Fact]
    public async Task SwitchingLanguageTellsTheViewTheNoteChanged()
    {
        var api = new FakeMessageApi();
        var screen = await OpenAsync(api);
        await api.LoseConnectionAsync(stillRetrying: true);
        var changed = new List<string?>();
        screen.PropertyChanged += (_, args) => changed.Add(args.PropertyName);

        screen.Locale = "it";

        Assert.Contains(nameof(screen.LiveUpdatesNote), changed);
    }

    [Fact]
    public async Task AnnouncementDuringAPageChangeDoesNotUndoIt()
    {
        var api = new FakeMessageApi();
        for (var index = 0; index < 51; index++)
        {
            api.AddMessage($"Message {index}", Kira, Odo);
        }

        var screen = await OpenAsync(api);
        api.HoldListLoads = true;
        var pageChange = screen.NextMessagesCommand.ExecuteAsync();
        await api.AnnounceAsync(api.AddMessage("Newest", Kira, Odo));

        api.ReleaseHeldLoads();
        await pageChange;

        Assert.Equal("Page 2 of 2 (52 in total)", screen.MessagePage);
    }

    [Fact]
    public async Task UnavailableLiveUpdatesAreExplainedAndTheListStillLoads()
    {
        var api = new FakeMessageApi { ListenFailure = new HttpRequestException("Upgrade refused.") };
        api.AddMessage("Supply run", Kira, Odo);

        var screen = await OpenAsync(api);

        Assert.Equal(
            new { Note = (string?)"Live updates are not available, so new messages appear only when the list is loaded again.", Count = 1 },
            new { Note = screen.LiveUpdatesNote, screen.Messages.Count });
    }

    private static async Task FilterAsync(MessageScreenViewModel screen, string name)
    {
        screen.FilterSearchText = name;
        await screen.FindCharactersCommand.ExecuteAsync();
        screen.SelectedFoundCharacter = screen.FoundCharacters[0];
    }

    private static async Task<MessageScreenViewModel> OpenAsync(FakeMessageApi api, FakeMessageDialogs? dialogs = null)
    {
        var screen = new MessageScreenViewModel(api, new JsonLocalizer(), dialogs ?? new FakeMessageDialogs(), "http://localhost/api", "en",
            FakeMessageApi.MessagesHref);
        await screen.LoadAsync();
        return screen;
    }
}
