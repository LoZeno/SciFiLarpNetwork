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
    public async Task UnavailableLiveUpdatesAreExplainedAndTheListStillLoads()
    {
        var api = new FakeMessageApi { ListenFailure = new HttpRequestException("Upgrade refused.") };
        api.AddMessage("Supply run", Kira, Odo);

        var screen = await OpenAsync(api);

        Assert.Equal(
            new { Status = (string?)"Live updates are not available, so new messages appear only when the list is loaded again.", Count = 1 },
            new { Status = screen.StatusMessage, screen.Messages.Count });
    }

    private static async Task FilterAsync(MessageScreenViewModel screen, string name)
    {
        screen.FilterSearchText = name;
        await screen.FindCharactersCommand.ExecuteAsync();
        screen.SelectedFoundCharacter = screen.FoundCharacters[0];
    }

    private static async Task<MessageScreenViewModel> OpenAsync(FakeMessageApi api)
    {
        var screen = new MessageScreenViewModel(api, new JsonLocalizer(), new FakeMessageDialogs(), "http://localhost/api", "en",
            FakeMessageApi.MessagesHref);
        await screen.LoadAsync();
        return screen;
    }
}
