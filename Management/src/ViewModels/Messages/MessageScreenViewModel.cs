using System.Collections.ObjectModel;
using System.Globalization;
using SciFiLarpNetwork.Common.Siren;
using SciFiLarpNetwork.Management.Api;
using SciFiLarpNetwork.Management.Api.Messages;
using SciFiLarpNetwork.Management.Api.Players;
using SciFiLarpNetwork.Management.Api.Skills;
using SciFiLarpNetwork.Management.Localization;
using SciFiLarpNetwork.Management.ViewModels.LarpEvents;

namespace SciFiLarpNetwork.Management.ViewModels.Messages;

/// <summary>
/// The messages screen: a paged message list, of every message or of one character's incoming messages, and the
/// selected message's details. Opening a message is recorded by the API. Sending follows the actions the API
/// advertises, which it offers only to administrators.
/// </summary>
public sealed class MessageScreenViewModel : ScreenViewModel
{
    private const string ReplyPrefix = "Re: ";
    private readonly IMessageDialogService _dialogs;
    private readonly string _messagesHref;
    private readonly AsyncCommand _reloadCommand;
    private readonly AsyncCommand _openMessageCommand;
    private readonly AsyncCommand _filterCommand;
    private SirenEntity? _messages;
    private SirenEntity? _message;
    private SirenEntity? _sent;
    private MessageCollectionProperties? _page;
    private MessageDetailsProperties? _details;
    private MessageListItem? _selectedMessage;
    private CharacterChoice? _filterCharacter;
    private CharacterChoice? _selectedFoundCharacter;
    private string _filterSearchText = string.Empty;
    private string? _filterSearchMessage;
    private int _listLoadGeneration;
    private int _detailLoadGeneration;

    public MessageScreenViewModel(
        IManagementApiClient apiClient,
        IManagementLocalizer localizer,
        IMessageDialogService dialogs,
        string apiBaseUrl,
        string locale,
        string messagesHref)
        : base(apiClient, localizer, apiBaseUrl, locale)
    {
        _dialogs = dialogs;
        _messagesHref = messagesHref;
        _reloadCommand = new AsyncCommand(LoadAsync, HandleUnexpectedFailure);
        _openMessageCommand = new AsyncCommand(() => RunAsync(() => OpenAsync(SelectedMessage?.Href)), HandleUnexpectedFailure);
        _filterCommand = new AsyncCommand(() => RunAsync(() => LoadMessagesCoreAsync(null)), HandleUnexpectedFailure);
        PreviousMessagesCommand = new AsyncCommand(
            () => RunAsync(() => LoadMessagesCoreAsync(_messages?.FindLinkHref(SirenRelations.Previous))),
            HandleUnexpectedFailure,
            () => !IsBusy && _messages?.FindLinkHref(SirenRelations.Previous) is not null);
        NextMessagesCommand = new AsyncCommand(
            () => RunAsync(() => LoadMessagesCoreAsync(_messages?.FindLinkHref(SirenRelations.Next))),
            HandleUnexpectedFailure,
            () => !IsBusy && _messages?.FindLinkHref(SirenRelations.Next) is not null);
        FindCharactersCommand = new AsyncCommand(() => RunAsync(FindCharactersAsync), HandleUnexpectedFailure,
            () => !IsBusy && !string.IsNullOrWhiteSpace(FilterSearchText));
        ShowAllMessagesCommand = new AsyncCommand(() => RunAsync(() => ApplyFilterAsync(null)), HandleUnexpectedFailure,
            () => !IsBusy && FilterCharacter is not null);
        NewMessageCommand = new AsyncCommand(() => RunAsync(NewMessageAsync), HandleUnexpectedFailure,
            () => !IsBusy && _messages?.FindAction("send-message") is not null);
        ReplyCommand = new AsyncCommand(() => RunAsync(ReplyAsync), HandleUnexpectedFailure,
            () => !IsBusy && _message?.FindAction("reply-to-message") is not null);
        OpenReplySourceCommand = new AsyncCommand(
            () => RunAsync(() => OpenAsync(_message?.FindLinkHref(SirenRelations.ReplySource))),
            HandleUnexpectedFailure,
            () => !IsBusy && _message?.FindLinkHref(SirenRelations.ReplySource) is not null);
    }

    public AsyncCommand PreviousMessagesCommand { get; }

    public AsyncCommand NextMessagesCommand { get; }

    public AsyncCommand FindCharactersCommand { get; }

    public AsyncCommand ShowAllMessagesCommand { get; }

    public AsyncCommand NewMessageCommand { get; }

    public AsyncCommand ReplyCommand { get; }

    public AsyncCommand OpenReplySourceCommand { get; }

    public ObservableCollection<MessageListItem> Messages { get; } = [];

    /// <summary>The characters found for the list filter; choosing one lists its incoming messages.</summary>
    public ObservableCollection<CharacterChoice> FoundCharacters { get; } = [];

    public bool HasFoundCharacters => FoundCharacters.Count > 0;

    public string MessagePage { get; private set; } = string.Empty;

    public MessageListItem? SelectedMessage
    {
        get => _selectedMessage;
        set
        {
            if (_selectedMessage == value)
            {
                return;
            }

            _selectedMessage = value;
            OnPropertyChanged();
            _openMessageCommand.Execute(null);
        }
    }

    public string FilterSearchText
    {
        get => _filterSearchText;
        set
        {
            if (_filterSearchText == value)
            {
                return;
            }

            _filterSearchText = value;
            OnPropertyChanged();
            FindCharactersCommand.NotifyCanExecuteChanged();
        }
    }

    public string? FilterSearchMessage
    {
        get => _filterSearchMessage;
        private set
        {
            if (_filterSearchMessage == value)
            {
                return;
            }

            _filterSearchMessage = value;
            OnPropertyChanged();
            OnPropertyChanged(nameof(HasFilterSearchMessage));
        }
    }

    public bool HasFilterSearchMessage => !string.IsNullOrEmpty(FilterSearchMessage);

    /// <summary>Choosing a found character lists that character's incoming messages.</summary>
    public CharacterChoice? SelectedFoundCharacter
    {
        get => _selectedFoundCharacter;
        set
        {
            if (_selectedFoundCharacter == value)
            {
                return;
            }

            _selectedFoundCharacter = value;
            OnPropertyChanged();
            if (value is not null && value != FilterCharacter)
            {
                _filterCharacter = value;
                OnPropertyChanged(nameof(FilterCharacter));
                OnPropertyChanged(nameof(FilterText));
                NotifyCommandsChanged();
                _filterCommand.Execute(null);
            }
        }
    }

    /// <summary>The character whose incoming messages are listed; <see langword="null"/> for every message.</summary>
    public CharacterChoice? FilterCharacter => _filterCharacter;

    public string FilterText => FilterCharacter is { } character
        ? string.Format(CultureInfo.CurrentCulture, Translate("messages.showingIncoming"), character.DisplayName)
        : Translate("messages.showingAll");

    public bool HasDetails => _details is not null;

    public string DetailTitle => _details?.Title ?? string.Empty;

    public string DetailSender => _details is { } details
        ? string.Format(CultureInfo.CurrentCulture, Translate("messages.from"), Describe(details.Sender))
        : string.Empty;

    public string DetailSentAt => _details is { } details
        ? string.Format(CultureInfo.CurrentCulture, Translate("messages.sentAt"), FormatTime(details.SentAtUtc))
        : string.Empty;

    public string DetailSentBy => _details is { } details
        ? string.Format(CultureInfo.CurrentCulture, Translate("messages.sentBy"), details.SentBy ?? "?")
        : string.Empty;

    public string DetailReplyTo => _details?.ReplyTo is { } source
        ? string.Format(CultureInfo.CurrentCulture, Translate("messages.inReplyTo"), source.Title)
        : string.Empty;

    public bool HasReplyTo => _details?.ReplyTo is not null;

    public string RecipientsHeading => string.Format(
        CultureInfo.CurrentCulture, Translate("messages.recipientsCount"), _details?.Recipients.Count ?? 0);

    public IReadOnlyList<string> DetailRecipients { get; private set; } = [];

    /// <summary>The text exactly as it was sent.</summary>
    public string DetailBody => _details?.Body ?? string.Empty;

    public string MessagesHeading => Translate("api.messages");

    public string DetailsHeading => Translate("messages.details");

    public string FilterSearchLabel => Translate("roster.search");

    public string FindLabel => Translate("players.search");

    public string ShowAllLabel => Translate("messages.allMessages");

    public string PreviousLabel => Translate("players.previousPage");

    public string NextLabel => Translate("players.nextPage");

    public string NewLabel => Translate("messages.new");

    public string ReplyLabel => Translate("api.replyToMessage");

    public string OpenReplySourceLabel => Translate("messages.openSource");

    public string? MessagesPlaceholder => _page is not null && Messages.Count == 0
        ? Translate(FilterCharacter is null ? "messages.noMessages" : "messages.noIncomingMessages")
        : null;

    public bool HasMessagesPlaceholder => MessagesPlaceholder is not null;

    public string? DetailsPlaceholder => _details is null ? Translate("messages.selectMessage") : null;

    public bool HasDetailsPlaceholder => DetailsPlaceholder is not null;

    public override Task LoadAsync() => RunAsync(() => LoadMessagesCoreAsync(null));

    private protected override void OnLocaleChanged()
    {
        foreach (var name in new[]
                 {
                     nameof(MessagesHeading), nameof(DetailsHeading), nameof(FilterSearchLabel), nameof(FindLabel),
                     nameof(ShowAllLabel), nameof(PreviousLabel), nameof(NextLabel), nameof(NewLabel), nameof(ReplyLabel),
                     nameof(OpenReplySourceLabel), nameof(FilterText)
                 })
        {
            OnPropertyChanged(name);
        }

        _reloadCommand.Execute(null);
    }

    private protected override void NotifyCommandsChanged()
    {
        foreach (var command in new[]
                 {
                     PreviousMessagesCommand, NextMessagesCommand, FindCharactersCommand, ShowAllMessagesCommand,
                     NewMessageCommand, ReplyCommand, OpenReplySourceCommand
                 })
        {
            command.NotifyCanExecuteChanged();
        }
    }

    /// <summary>
    /// Loads a page of the list: <paramref name="href"/> when following a page link, otherwise the first page of every
    /// message or of the filter character's incoming messages.
    /// </summary>
    private async Task LoadMessagesCoreAsync(string? href)
    {
        var generation = ++_listLoadGeneration;
        var collection = await GetAsync(href ?? _messagesHref);
        if (href is null && FilterCharacter is { } character)
        {
            var filter = collection.FindAction("filter-messages") ?? throw MissingAction();
            collection = await GetAsync(filter.ToQueryHref(new Dictionary<string, string?>
            {
                ["characterId"] = character.CharacterId.ToString("D")
            }));
        }

        if (generation != _listLoadGeneration)
        {
            return;
        }

        _messages = collection;
        _page = SirenProperties.Read<MessageCollectionProperties>(collection.Properties);
        IsLoaded = true;
        CanWrite = collection.FindAction("send-message") is not null;
        MessagePage = string.Format(CultureInfo.CurrentCulture, Translate("players.page"), _page.PageNumber, _page.PageCount, _page.TotalCount);
        OnPropertyChanged(nameof(MessagePage));
        var selectedId = SelectedMessage?.MessageId;
        ReplaceAll(Messages, (collection.Entities ?? []).Select(ToListItem));
        _selectedMessage = Messages.FirstOrDefault(item => item.MessageId == selectedId);
        OnPropertyChanged(nameof(SelectedMessage));
        OnPropertyChanged(nameof(MessagesPlaceholder));
        OnPropertyChanged(nameof(HasMessagesPlaceholder));
        NotifyCommandsChanged();
    }

    /// <summary>Opens a message's details, which the API records as an opening.</summary>
    private async Task OpenAsync(string? href)
    {
        var generation = ++_detailLoadGeneration;
        var resource = href is null ? null : await GetAsync(href);
        if (generation != _detailLoadGeneration)
        {
            return;
        }

        _message = resource;
        _details = resource is null ? null : SirenProperties.Read<MessageDetailsProperties>(resource.Properties);
        DetailRecipients = _details?.Recipients.Select(Describe).ToArray() ?? [];
        foreach (var name in new[]
                 {
                     nameof(HasDetails), nameof(DetailTitle), nameof(DetailSender), nameof(DetailSentAt), nameof(DetailSentBy),
                     nameof(DetailReplyTo), nameof(HasReplyTo), nameof(RecipientsHeading), nameof(DetailRecipients),
                     nameof(DetailBody), nameof(DetailsPlaceholder), nameof(HasDetailsPlaceholder)
                 })
        {
            OnPropertyChanged(name);
        }

        NotifyCommandsChanged();
    }

    private async Task FindCharactersAsync()
    {
        FilterSearchMessage = null;
        var found = await SearchCharactersAsync(FilterSearchText);
        _selectedFoundCharacter = null;
        ReplaceAll(FoundCharacters, found);
        OnPropertyChanged(nameof(SelectedFoundCharacter));
        OnPropertyChanged(nameof(HasFoundCharacters));
        FilterSearchMessage = found.Count == 0 ? Translate("common.noResults") : null;
    }

    private async Task ApplyFilterAsync(CharacterChoice? character)
    {
        _filterCharacter = character;
        _selectedFoundCharacter = null;
        OnPropertyChanged(nameof(FilterCharacter));
        OnPropertyChanged(nameof(SelectedFoundCharacter));
        OnPropertyChanged(nameof(FilterText));
        await LoadMessagesCoreAsync(null);
    }

    private async Task<IReadOnlyList<CharacterChoice>> SearchCharactersAsync(string text)
    {
        var characters = await GetAsync(_messages?.FindLinkHref(SirenRelations.Characters) ?? throw MissingAction());
        var search = characters.FindAction("search-characters") ?? throw MissingAction();
        var results = await GetAsync(search.ToQueryHref(new Dictionary<string, string?> { ["search"] = text }));
        return (results.Entities ?? [])
            .Select(entity => SirenProperties.Read<CharacterProperties>(entity.Properties))
            .Select(character => new CharacterChoice(
                character.CharacterId,
                $"#{character.Number} {character.Name} — {character.PlayerDisplayName}"))
            .ToArray();
    }

    private async Task NewMessageAsync()
    {
        var action = _messages?.FindAction("send-message") ?? throw MissingAction();
        var editor = CreateEditor("messages.newMessage", null, action);
        if (FilterCharacter is { } character)
        {
            editor.AddRecipient(character);
        }

        await _dialogs.ShowMessageEditorAsync(editor);
        await ReloadAfterSendAsync(editor);
    }

    /// <summary>
    /// Replies to the message shown: from the listed character, or else the first recipient, to the sender, with the
    /// title marked as a reply. Every field can be changed in the dialog.
    /// </summary>
    private async Task ReplyAsync()
    {
        var action = _message?.FindAction("reply-to-message") ?? throw MissingAction();
        var source = _details ?? throw MissingAction();
        var editor = CreateEditor("messages.replyTitle", source.MessageId, action);
        var fromListed = source.Recipients.FirstOrDefault(recipient => recipient.CharacterId == FilterCharacter?.CharacterId)
                         ?? (source.Recipients.Count > 0 ? source.Recipients[0] : null);
        editor.Sender = fromListed is null ? null : MessageCharacterNames.Choice(fromListed, Translate);
        editor.AddRecipient(MessageCharacterNames.Choice(source.Sender, Translate));
        editor.MessageTitle = source.Title.StartsWith(ReplyPrefix, StringComparison.OrdinalIgnoreCase)
            ? source.Title
            : $"{ReplyPrefix}{source.Title}";
        await _dialogs.ShowMessageEditorAsync(editor);
        await ReloadAfterSendAsync(editor);
    }

    private MessageEditorViewModel CreateEditor(string titleKey, Guid? replyToMessageId, SirenAction action)
    {
        var page = _page ?? throw MissingAction();
        return new MessageEditorViewModel(
            Localizer,
            Locale,
            titleKey,
            new MessageLimits(page.MaximumRecipients, page.MaximumTitleLength, page.MaximumBodyLength),
            replyToMessageId,
            SearchCharactersAsync,
            editor => SubmitEditorAsync(editor, async () => _sent = await SubmitAsync(action, editor.CreateBody()), null));
    }

    /// <summary>After a send, reloads the list and opens the message that was sent.</summary>
    private async Task ReloadAfterSendAsync(MessageEditorViewModel editor)
    {
        if (!editor.IsSaved || _sent?.FindLinkHref("self") is not { } sent)
        {
            return;
        }

        _sent = null;
        await LoadMessagesCoreAsync(null);
        _selectedMessage = Messages.FirstOrDefault(item => item.MessageId == editor.MessageId);
        OnPropertyChanged(nameof(SelectedMessage));
        await OpenAsync(sent);
        StatusMessage = Translate("actions.completed");
    }

    private string Describe(MessageCharacterProperties character) => MessageCharacterNames.Describe(character, Translate);

    private static string FormatTime(DateTimeOffset value) => value.ToLocalTime().ToString("g", CultureInfo.CurrentCulture);

    private MessageListItem ToListItem(SirenSubEntity entity)
    {
        var message = SirenProperties.Read<MessageSummaryProperties>(entity.Properties);
        return new MessageListItem(
            message.MessageId,
            entity.Href ?? throw MissingAction(),
            string.Format(
                CultureInfo.CurrentCulture,
                Translate("messages.listEntry"),
                FormatTime(message.SentAtUtc),
                Describe(message.Sender),
                message.Title,
                message.RecipientCount));
    }
}
