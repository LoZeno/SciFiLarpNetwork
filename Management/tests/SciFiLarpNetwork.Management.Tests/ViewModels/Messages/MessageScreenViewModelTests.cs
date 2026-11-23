using System.Net;
using System.Text.Json;
using SciFiLarpNetwork.Management.Api;
using SciFiLarpNetwork.Management.Localization;
using SciFiLarpNetwork.Management.ViewModels.Messages;

namespace SciFiLarpNetwork.Management.Tests.ViewModels.Messages;

public sealed class MessageScreenViewModelTests
{
    private static readonly Guid Kira = FakeMessageApi.KiraId;
    private static readonly Guid Odo = FakeMessageApi.OdoId;
    private static readonly Guid Jade = FakeMessageApi.JadeId;

    [Fact]
    public async Task LoadListsMessagesNewestFirst()
    {
        var api = new FakeMessageApi();
        api.AddMessage("First", Kira, Odo);
        api.AddMessage("Second", Odo, Kira, Jade);

        var screen = await OpenAsync(api);

        Assert.Equal(
            [
                $"{Time(api.Messages[1].SentAtUtc)} · #2 Odo · Second · recipients: 2",
                $"{Time(api.Messages[0].SentAtUtc)} · #1 Kira · First · recipients: 1"
            ],
            screen.Messages.Select(message => message.DisplayName));
    }

    [Fact]
    public async Task EmptyListExplainsThereAreNoMessages()
    {
        var screen = await OpenAsync(new FakeMessageApi());

        Assert.Equal("No messages yet.", screen.MessagesPlaceholder);
    }

    [Fact]
    public async Task ReaderCannotSendOrReply()
    {
        var api = new FakeMessageApi { CanWrite = false };
        api.AddMessage("First", Kira, Odo);
        var screen = await OpenAsync(api);

        screen.SelectedMessage = screen.Messages[0];

        Assert.Equal(
            new { CanWrite = false, CanSend = false, CanReply = false },
            new { screen.CanWrite, CanSend = screen.NewMessageCommand.CanExecute(null), CanReply = screen.ReplyCommand.CanExecute(null) });
    }

    [Fact]
    public async Task SelectingAMessageOpensItsDetails()
    {
        var api = new FakeMessageApi();
        var message = api.AddMessage("Supply run", Kira, Odo, Jade);
        var screen = await OpenAsync(api);

        screen.SelectedMessage = screen.Messages[0];

        Assert.Equal(
            new
            {
                Title = "Supply run",
                Sender = "From: #1 Kira",
                SentAt = $"Sent: {Time(message.SentAtUtc)}",
                SentBy = "Sent by administrator: local-admin",
                RecipientsHeading = "Recipients (2)",
                Body = "Body of Supply run",
                HasReplyTo = false
            },
            new
            {
                Title = screen.DetailTitle,
                Sender = screen.DetailSender,
                SentAt = screen.DetailSentAt,
                SentBy = screen.DetailSentBy,
                screen.RecipientsHeading,
                Body = screen.DetailBody,
                screen.HasReplyTo
            });
    }

    [Fact]
    public async Task EveryOpeningAsksTheApiForTheDetails()
    {
        var api = new FakeMessageApi();
        api.AddMessage("First", Kira, Odo);
        api.AddMessage("Second", Kira, Odo);
        var screen = await OpenAsync(api);

        screen.SelectedMessage = screen.Messages[0];
        screen.SelectedMessage = screen.Messages[1];
        screen.SelectedMessage = screen.Messages[0];

        Assert.Equal(3, api.Openings.Count);
    }

    [Fact]
    public async Task DetailsMarkArchivedAndUnknownCharacters()
    {
        var api = new FakeMessageApi();
        api.ArchivedCharacters.Add(Odo);
        api.AddMessage("Supply run", Kira, Odo, Guid.NewGuid());
        var screen = await OpenAsync(api);

        screen.SelectedMessage = screen.Messages[0];

        Assert.Equal(["#2 Odo (Archived)", "Unknown character"], screen.DetailRecipients);
    }

    [Fact]
    public async Task LongBodyIsShownUnchanged()
    {
        var api = new FakeMessageApi();
        var body = string.Join("\r\n", Enumerable.Range(0, 500).Select(line => $"  Line {line} — àèìòù  "));
        api.Messages.Add(new FakeMessage(Guid.NewGuid(), Kira, [Odo], "Long", body, DateTimeOffset.UnixEpoch, null));
        var screen = await OpenAsync(api);

        screen.SelectedMessage = screen.Messages[0];

        Assert.Equal(body, screen.DetailBody);
    }

    [Fact]
    public async Task NoMessageSelectedAsksToSelectOne()
    {
        var api = new FakeMessageApi();
        api.AddMessage("First", Kira, Odo);

        var screen = await OpenAsync(api);

        Assert.Equal("Select a message to read it.", screen.DetailsPlaceholder);
    }

    [Fact]
    public async Task NextLoadsTheFollowingPage()
    {
        var api = new FakeMessageApi();
        for (var index = 0; index < 51; index++)
        {
            api.AddMessage($"Message {index}", Kira, Odo);
        }

        var screen = await OpenAsync(api);

        await screen.NextMessagesCommand.ExecuteAsync();

        Assert.Equal(new { Page = "Page 2 of 2 (51 in total)", Title = "Message 0" },
            new { Page = screen.MessagePage, Title = screen.Messages.Single().DisplayName.Split(" · ")[2] });
    }

    [Fact]
    public async Task ChoosingAFoundCharacterListsItsIncomingMessages()
    {
        var api = new FakeMessageApi();
        api.AddMessage("To Odo", Kira, Odo);
        api.AddMessage("To Kira", Odo, Kira);
        var screen = await OpenAsync(api);

        await FilterAsync(screen, "Kira");

        Assert.Equal(
            new { Filter = "Showing the incoming messages of #1 Kira — Ann.", Titles = "To Kira" },
            new { Filter = screen.FilterText, Titles = string.Join(',', screen.Messages.Select(message => message.DisplayName.Split(" · ")[2])) });
    }

    [Fact]
    public async Task FilteredCharacterWithoutMessagesSaysSo()
    {
        var api = new FakeMessageApi();
        api.AddMessage("To Odo", Kira, Odo);
        var screen = await OpenAsync(api);

        await FilterAsync(screen, "Jade");

        Assert.Equal("This character has no incoming messages.", screen.MessagesPlaceholder);
    }

    [Fact]
    public async Task SearchWithoutResultsSaysSo()
    {
        var screen = await OpenAsync(new FakeMessageApi());
        screen.FilterSearchText = "Nobody";

        await screen.FindCharactersCommand.ExecuteAsync();

        Assert.Equal("No results found.", screen.FilterSearchMessage);
    }

    [Fact]
    public async Task ShowAllMessagesClearsTheFilter()
    {
        var api = new FakeMessageApi();
        api.AddMessage("To Odo", Kira, Odo);
        api.AddMessage("To Kira", Odo, Kira);
        var screen = await OpenAsync(api);
        await FilterAsync(screen, "Kira");

        await screen.ShowAllMessagesCommand.ExecuteAsync();

        Assert.Equal(new { Filter = "Showing all messages.", Count = 2 }, new { Filter = screen.FilterText, screen.Messages.Count });
    }

    [Fact]
    public async Task NewMessageSendsTheChosenSenderRecipientsTitleAndText()
    {
        var api = new FakeMessageApi();
        var dialogs = new FakeMessageDialogs { OnMessageEditor = editor => WriteAsync(editor, "Kira", ["Odo", "Jade"]) };
        var screen = await OpenAsync(api, dialogs);

        await screen.NewMessageCommand.ExecuteAsync();

        var body = JsonDocument.Parse(api.Submissions.Single().Body!).RootElement;
        Assert.Equal(
            new
            {
                Action = "send-message",
                MessageId = dialogs.LastEditor!.MessageId,
                Sender = Kira,
                Recipients = $"{Odo},{Jade}",
                Title = (string?)"Supply run",
                Text = (string?)"Meet at the dock.\nBring crates.",
                ReplyTo = JsonValueKind.Null
            },
            new
            {
                Action = api.Submissions.Single().ActionName,
                MessageId = body.GetProperty("messageId").GetGuid(),
                Sender = body.GetProperty("senderId").GetGuid(),
                Recipients = string.Join(',', body.GetProperty("recipientIds").EnumerateArray().Select(id => id.GetGuid())),
                Title = body.GetProperty("title").GetString(),
                Text = body.GetProperty("body").GetString(),
                ReplyTo = body.GetProperty("replyToMessageId").ValueKind
            });
    }

    [Fact]
    public async Task SentMessageIsOpenedAfterTheDialogCloses()
    {
        var api = new FakeMessageApi();
        var screen = await OpenAsync(api, new FakeMessageDialogs { OnMessageEditor = editor => WriteAsync(editor, "Kira", ["Odo"]) });

        await screen.NewMessageCommand.ExecuteAsync();

        Assert.Equal(new { Title = "Supply run", Selected = (Guid?)api.Messages.Single().Id },
            new { Title = screen.DetailTitle, Selected = screen.SelectedMessage?.MessageId });
    }

    [Fact]
    public async Task SendingDoesNotCountAsAnOpening()
    {
        var api = new FakeMessageApi();
        var screen = await OpenAsync(api, new FakeMessageDialogs { OnMessageEditor = editor => WriteAsync(editor, "Kira", ["Odo"]) });

        await screen.NewMessageCommand.ExecuteAsync();

        Assert.Empty(api.Openings);
    }

    [Fact]
    public async Task NewMessageFromAFilteredListStartsWithThatCharacterAsRecipient()
    {
        var api = new FakeMessageApi();
        var dialogs = new FakeMessageDialogs();
        var screen = await OpenAsync(api, dialogs);
        await FilterAsync(screen, "Odo");

        await screen.NewMessageCommand.ExecuteAsync();

        Assert.Equal([Odo], dialogs.LastEditor!.Recipients.Select(recipient => recipient.CharacterId));
    }

    [Fact]
    public async Task CancelledDialogSendsNothing()
    {
        var api = new FakeMessageApi();
        var screen = await OpenAsync(api, new FakeMessageDialogs());

        await screen.NewMessageCommand.ExecuteAsync();

        Assert.Empty(api.Submissions);
    }

    [Fact]
    public async Task FailedSendShowsTheErrorInTheDialog()
    {
        var api = new FakeMessageApi();
        api.Failures["send-message"] = new ManagementApiException("errors.archivedCharacters", HttpStatusCode.Conflict);
        var dialogs = new FakeMessageDialogs { OnMessageEditor = editor => WriteAsync(editor, "Kira", ["Odo"]) };
        var screen = await OpenAsync(api, dialogs);

        await screen.NewMessageCommand.ExecuteAsync();

        Assert.Equal(
            "Archived characters cannot send or receive messages. Remove them or unarchive them first.",
            dialogs.LastEditor!.ErrorMessage);
    }

    [Fact]
    public async Task SavingAgainAfterAFailureSendsTheSameMessageIdentifier()
    {
        var api = new FakeMessageApi();
        api.Failures["send-message"] = new HttpRequestException("Connection lost.");
        var dialogs = new FakeMessageDialogs
        {
            OnMessageEditor = async editor =>
            {
                await WriteAsync(editor, "Kira", ["Odo"]);
                await editor.SaveCommand.ExecuteAsync();
            }
        };
        var screen = await OpenAsync(api, dialogs);

        await screen.NewMessageCommand.ExecuteAsync();

        Assert.Single(api.Submissions.Select(submission => JsonDocument.Parse(submission.Body!).RootElement.GetProperty("messageId").GetGuid()).Distinct());
    }

    [Fact]
    public async Task ReplyStartsFromTheFirstRecipientToTheSenderWithAReplyTitle()
    {
        var api = new FakeMessageApi();
        api.AddMessage("Supply run", Kira, Odo, Jade);
        var dialogs = new FakeMessageDialogs();
        var screen = await OpenAsync(api, dialogs);
        screen.SelectedMessage = screen.Messages[0];

        await screen.ReplyCommand.ExecuteAsync();

        var editor = dialogs.LastEditor!;
        Assert.Equal(
            new { Sender = (Guid?)Odo, Recipients = Kira.ToString(), Title = "Re: Supply run", Heading = "Reply to message" },
            new
            {
                Sender = editor.Sender?.CharacterId,
                Recipients = string.Join(',', editor.Recipients.Select(recipient => recipient.CharacterId)),
                Title = editor.MessageTitle,
                Heading = editor.Title
            });
    }

    [Fact]
    public async Task ReplyFromAFilteredListIsSentByTheListedCharacter()
    {
        var api = new FakeMessageApi();
        api.AddMessage("Supply run", Kira, Odo, Jade);
        var dialogs = new FakeMessageDialogs();
        var screen = await OpenAsync(api, dialogs);
        await FilterAsync(screen, "Jade");
        screen.SelectedMessage = screen.Messages[0];

        await screen.ReplyCommand.ExecuteAsync();

        Assert.Equal(Jade, dialogs.LastEditor!.Sender?.CharacterId);
    }

    [Fact]
    public async Task ReplyTitleIsCutToTheTitleLimit()
    {
        var api = new FakeMessageApi();
        api.AddMessage(new string('t', 126), Kira, Odo);
        var dialogs = new FakeMessageDialogs();
        var screen = await OpenAsync(api, dialogs);
        screen.SelectedMessage = screen.Messages[0];

        await screen.ReplyCommand.ExecuteAsync();

        Assert.Equal($"Re: {new string('t', 124)}", dialogs.LastEditor!.MessageTitle);
    }

    [Fact]
    public async Task ReplyToAReplyKeepsASinglePrefix()
    {
        var api = new FakeMessageApi();
        api.AddMessage("Re: Supply run", Kira, Odo);
        var dialogs = new FakeMessageDialogs();
        var screen = await OpenAsync(api, dialogs);
        screen.SelectedMessage = screen.Messages[0];

        await screen.ReplyCommand.ExecuteAsync();

        Assert.Equal("Re: Supply run", dialogs.LastEditor!.MessageTitle);
    }

    [Fact]
    public async Task ReplySendsTheSourceMessage()
    {
        var api = new FakeMessageApi();
        var source = api.AddMessage("Supply run", Kira, Odo);
        var screen = await OpenAsync(api, new FakeMessageDialogs { OnMessageEditor = editor => editor.SaveCommand.ExecuteAsync() });
        screen.SelectedMessage = screen.Messages[0];

        await screen.ReplyCommand.ExecuteAsync();

        Assert.Equal(source.Id,
            JsonDocument.Parse(api.Submissions.Single().Body!).RootElement.GetProperty("replyToMessageId").GetGuid());
    }

    [Fact]
    public async Task ReplyShowsItsSourceAndOpensIt()
    {
        var api = new FakeMessageApi();
        var source = api.AddMessage("Supply run", Kira, Odo);
        api.Messages.Add(new FakeMessage(Guid.NewGuid(), Odo, [Kira], "Re: Supply run", "On my way.", DateTimeOffset.UnixEpoch.AddYears(60), source.Id));
        var screen = await OpenAsync(api);
        screen.SelectedMessage = screen.Messages.Single(message => message.MessageId != source.Id);
        var replyTo = screen.DetailReplyTo;

        await screen.OpenReplySourceCommand.ExecuteAsync();

        Assert.Equal(new { ReplyTo = "In reply to: Supply run", Title = "Supply run" }, new { ReplyTo = replyTo, Title = screen.DetailTitle });
    }

    [Fact]
    public async Task SwitchingLanguageRelabelsTheOpenMessageWithoutOpeningItAgain()
    {
        var api = new FakeMessageApi();
        api.ArchivedCharacters.Add(Odo);
        api.AddMessage("Supply run", Kira, Odo, Guid.NewGuid());
        var screen = await OpenAsync(api);
        screen.SelectedMessage = screen.Messages[0];

        screen.Locale = "it";

        Assert.Equal(
            new
            {
                Sender = "Da: #1 Kira",
                Heading = "Destinatari (2)",
                Recipients = "#2 Odo (Archiviato), Personaggio sconosciuto",
                Openings = 1
            },
            new
            {
                Sender = screen.DetailSender,
                Heading = screen.RecipientsHeading,
                Recipients = string.Join(", ", screen.DetailRecipients),
                Openings = api.Openings.Count
            });
    }

    [Fact]
    public async Task SwitchingLanguageTellsTheViewEveryDetailChanged()
    {
        var api = new FakeMessageApi();
        api.AddMessage("Supply run", Kira, Odo);
        var screen = await OpenAsync(api);
        screen.SelectedMessage = screen.Messages[0];
        var changed = new HashSet<string?>();
        screen.PropertyChanged += (_, args) => changed.Add(args.PropertyName);

        screen.Locale = "it";

        Assert.Superset(
            new HashSet<string?>
            {
                nameof(screen.DetailSender), nameof(screen.DetailSentAt), nameof(screen.DetailSentBy), nameof(screen.DetailReplyTo),
                nameof(screen.RecipientsHeading), nameof(screen.DetailRecipients), nameof(screen.DetailsPlaceholder)
            },
            changed);
    }

    [Fact]
    public async Task SwitchingLanguageRelabelsTheDetailsPlaceholder()
    {
        var screen = await OpenAsync(new FakeMessageApi());

        screen.Locale = "it";

        Assert.Equal("Seleziona un messaggio per leggerlo.", screen.DetailsPlaceholder);
    }

    [Fact]
    public async Task ItalianLabelsAgreeWithMessaggio()
    {
        var screen = await OpenAsync(new FakeMessageApi(), locale: "it");

        Assert.Equal(new { New = "Nuovo", Empty = (string?)"Ancora nessun messaggio." }, new { New = screen.NewLabel, Empty = screen.MessagesPlaceholder });
    }

    internal static async Task WriteAsync(MessageEditorViewModel editor, string sender, IReadOnlyList<string> recipients)
    {
        editor.SearchText = sender;
        await editor.SearchCommand.ExecuteAsync();
        editor.SetSenderCommand.Execute(null);
        foreach (var recipient in recipients)
        {
            editor.SearchText = recipient;
            await editor.SearchCommand.ExecuteAsync();
            editor.AddRecipientCommand.Execute(null);
        }

        editor.MessageTitle = "Supply run";
        editor.Body = "Meet at the dock.\nBring crates.";
        await editor.SaveCommand.ExecuteAsync();
    }

    private static async Task FilterAsync(MessageScreenViewModel screen, string name)
    {
        screen.FilterSearchText = name;
        await screen.FindCharactersCommand.ExecuteAsync();
        screen.SelectedFoundCharacter = screen.FoundCharacters[0];
    }

    private static string Time(DateTimeOffset value) => value.ToLocalTime().ToString("g", System.Globalization.CultureInfo.CurrentCulture);

    private static async Task<MessageScreenViewModel> OpenAsync(FakeMessageApi api, FakeMessageDialogs? dialogs = null, string locale = "en")
    {
        var screen = new MessageScreenViewModel(api, new JsonLocalizer(), dialogs ?? new FakeMessageDialogs(), "http://localhost/api", locale,
            FakeMessageApi.MessagesHref);
        await screen.LoadAsync();
        return screen;
    }
}
