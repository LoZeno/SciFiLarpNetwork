using System.Collections.ObjectModel;
using System.Diagnostics;
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
/// advertises, which it offers only to administrators. While the screen is open it listens to the API's live updates
/// and reloads the page it shows when a sent message belongs on it; closing the screen stops listening.
/// </summary>
public sealed class MessageScreenViewModel : ScreenViewModel, IDisposable
{
    private const string ReplyPrefix = "Re: ";
    private readonly IMessageDialogService _dialogs;
    private readonly string _messagesHref;
    private readonly AsyncCommand _reloadCommand;
    private readonly AsyncCommand _openMessageCommand;
    private readonly AsyncCommand _filterCommand;
    private readonly AsyncCommand _liveReloadCommand;
    private SynchronizationContext? _uiContext;
    private IAsyncDisposable? _liveUpdates;
    private bool _isListening;
    private bool _isDisposed;
    private string? _liveUpdatesNoteKey;
    private bool _liveReloadPending;
    private int _userListLoads;
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
        _liveReloadCommand = new AsyncCommand(ReloadShownPageAsync, HandleUnexpectedFailure);
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

    /// <summary>
    /// Whether live updates are interrupted or unavailable. It has its own line, apart from <see cref="StatusMessage"/>,
    /// so the user's own actions do not hide it and a reconnect clears nothing else.
    /// </summary>
    public string? LiveUpdatesNote => _liveUpdatesNoteKey is { } key ? Translate(key) : null;

    public bool HasLiveUpdatesNote => _liveUpdatesNoteKey is not null;

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

    public override Task LoadAsync() => RunAsync(async () =>
    {
        await LoadMessagesCoreAsync(null);
        await StartListeningAsync();
    });

    /// <summary>Stops listening to live updates. The connection closes in the background.</summary>
    public void Dispose()
    {
        _isDisposed = true;
        if (Interlocked.Exchange(ref _liveUpdates, null) is { } liveUpdates)
        {
            _ = CloseAsync(liveUpdates);
        }
    }

    private protected override void OnLocaleChanged()
    {
        foreach (var name in new[]
                 {
                     nameof(MessagesHeading), nameof(DetailsHeading), nameof(FilterSearchLabel), nameof(FindLabel),
                     nameof(ShowAllLabel), nameof(PreviousLabel), nameof(NextLabel), nameof(NewLabel), nameof(ReplyLabel),
                     nameof(OpenReplySourceLabel), nameof(FilterText), nameof(LiveUpdatesNote)
                 })
        {
            OnPropertyChanged(name);
        }

        RefreshDetails();
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
    /// message or of the filter character's incoming messages. Only the newest load is shown, so a live reload asked
    /// for while the user's own load runs waits for it; otherwise its older page or filter could replace the user's.
    /// </summary>
    private async Task LoadMessagesCoreAsync(string? href, bool isLiveReload = false)
    {
        if (isLiveReload)
        {
            await LoadPageAsync(href);
            return;
        }

        _userListLoads++;
        try
        {
            await LoadPageAsync(href);
        }
        finally
        {
            if (--_userListLoads == 0 && _liveReloadPending)
            {
                _liveReloadPending = false;
                _liveReloadCommand.Execute(null);
            }
        }
    }

    private async Task LoadPageAsync(string? href)
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

    /// <summary>
    /// Starts listening once, when the list advertises live updates. Without them the screen still works and shows a
    /// note that new messages appear only when the list is loaded again.
    /// </summary>
    private async Task StartListeningAsync()
    {
        if (_isListening || _isDisposed || _messages?.FindLinkHref(SirenRelations.LiveUpdates) is not { } href)
        {
            return;
        }

        _isListening = true;
        _uiContext = SynchronizationContext.Current;
        try
        {
            var liveUpdates = await ApiClient.ListenForMessagesAsync(ApiBaseUrl, href,
                new MessageLiveHandlers(OnMessageSentAsync, OnReconnectedAsync, OnConnectionLostAsync));
            if (_isDisposed)
            {
                await CloseAsync(liveUpdates);
                return;
            }

            _liveUpdates = liveUpdates;
            ShowLiveUpdatesNote(null);
        }
        catch (Exception failure) when (failure is not OperationCanceledException)
        {
            Trace.TraceWarning("Live message updates are unavailable: {0}", failure);
            _isListening = false;
            ShowLiveUpdatesNote("messages.liveUpdatesUnavailable");
        }
    }

    /// <summary>Reloads the shown page when the message belongs on it: every message, or the listed character's incoming ones.</summary>
    private Task OnMessageSentAsync(MessageSentNotice notice)
    {
        if (FilterCharacter is not { } character || notice.RecipientIds.Contains(character.CharacterId))
        {
            OnScreen(() => _liveReloadCommand.Execute(null));
        }

        return Task.CompletedTask;
    }

    /// <summary>
    /// Notices sent while the connection was down are lost, so the shown page is loaded again; the interruption note,
    /// if it is still shown, goes away.
    /// </summary>
    private Task OnReconnectedAsync()
    {
        OnScreen(() =>
        {
            if (_liveUpdatesNoteKey == "messages.liveUpdatesInterrupted")
            {
                ShowLiveUpdatesNote(null);
            }

            _liveReloadCommand.Execute(null);
        });
        return Task.CompletedTask;
    }

    /// <summary>
    /// Says that live updates stopped. While the connection is still retrying the note says so; once it has stopped
    /// for good the connection is released, so the next load of the screen listens again.
    /// </summary>
    private Task OnConnectionLostAsync(bool stillRetrying)
    {
        OnScreen(() =>
        {
            if (_isDisposed)
            {
                return;
            }

            ShowLiveUpdatesNote(stillRetrying ? "messages.liveUpdatesInterrupted" : "messages.liveUpdatesUnavailable");
            if (stillRetrying)
            {
                return;
            }

            _isListening = false;
            if (Interlocked.Exchange(ref _liveUpdates, null) is { } liveUpdates)
            {
                _ = CloseAsync(liveUpdates);
            }
        });
        return Task.CompletedTask;
    }

    private void ShowLiveUpdatesNote(string? key)
    {
        _liveUpdatesNoteKey = key;
        OnPropertyChanged(nameof(LiveUpdatesNote));
        OnPropertyChanged(nameof(HasLiveUpdatesNote));
    }

    /// <summary>Live updates arrive on a background thread; the screen changes on the thread that opened it.</summary>
    private void OnScreen(Action change)
    {
        if (_uiContext is { } context)
        {
            context.Post(_ => change(), null);
        }
        else
        {
            change();
        }
    }

    /// <summary>
    /// Loads the page the user is on again, with its filter, keeping the selected message and its details. It does not
    /// mark the screen busy or clear its status, so a live update does not interrupt the user.
    /// </summary>
    private async Task ReloadShownPageAsync()
    {
        if (_isDisposed || _messages?.FindLinkHref("self") is not { } shown)
        {
            return;
        }

        if (_userListLoads > 0)
        {
            _liveReloadPending = true;
            return;
        }

        try
        {
            await LoadMessagesCoreAsync(shown, isLiveReload: true);
        }
        catch (Exception failure) when (IsExpectedFailure(failure))
        {
            StatusMessage = Report(failure);
        }
    }

    private static async Task CloseAsync(IAsyncDisposable liveUpdates)
    {
        try
        {
            await liveUpdates.DisposeAsync();
        }
        catch (Exception failure)
        {
            Trace.TraceWarning("Closing the live message updates failed: {0}", failure);
        }
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

        ShowDetails(resource);
    }

    private void ShowDetails(SirenEntity? resource)
    {
        _message = resource;
        _details = resource is null ? null : SirenProperties.Read<MessageDetailsProperties>(resource.Properties);
        RefreshDetails();
        NotifyCommandsChanged();
    }

    /// <summary>
    /// Rebuilds the details texts from the message already shown. A language switch uses this instead of fetching the
    /// message again, which the API would record as another opening.
    /// </summary>
    private void RefreshDetails()
    {
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
        var title = source.Title.StartsWith(ReplyPrefix, StringComparison.OrdinalIgnoreCase)
            ? source.Title
            : $"{ReplyPrefix}{source.Title}";
        editor.MessageTitle = CutTitle(title, editor.MaximumTitleLength);
        await _dialogs.ShowMessageEditorAsync(editor);
        await ReloadAfterSendAsync(editor);
    }

    /// <summary>
    /// Cuts a prefilled title to the limit without splitting a character made of two UTF-16 units, such as an emoji;
    /// half of one cannot be stored.
    /// </summary>
    private static string CutTitle(string title, int maximumLength)
    {
        if (title.Length <= maximumLength)
        {
            return title;
        }

        var length = char.IsHighSurrogate(title[maximumLength - 1]) ? maximumLength - 1 : maximumLength;
        return title[..length];
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

    /// <summary>
    /// After a send, reloads the list and shows the message that was sent. The send's answer already holds its
    /// details, so it is shown without opening it: the record of openings lists only deliberate inspections.
    /// </summary>
    private async Task ReloadAfterSendAsync(MessageEditorViewModel editor)
    {
        if (!editor.IsSaved || _sent is not { } sent)
        {
            return;
        }

        _sent = null;
        await LoadMessagesCoreAsync(null);
        _selectedMessage = Messages.FirstOrDefault(item => item.MessageId == editor.MessageId);
        OnPropertyChanged(nameof(SelectedMessage));
        ++_detailLoadGeneration;
        ShowDetails(sent);
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
