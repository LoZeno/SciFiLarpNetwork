using System.Collections.ObjectModel;
using System.Diagnostics;
using System.Globalization;
using System.Text.Json;
using SciFiLarpNetwork.Management.Localization;
using SciFiLarpNetwork.Management.ViewModels.LarpEvents;

namespace SciFiLarpNetwork.Management.ViewModels.Messages;

/// <summary>
/// The message dialog, for a new message or a reply: find characters, choose the sender and the recipients, and write
/// the title and text. The message identifier is chosen once when the dialog opens, so saving again after a failure
/// never sends the message twice.
/// </summary>
public sealed class MessageEditorViewModel : CatalogEditorViewModel
{
    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web);
    private readonly Func<string, Task<IReadOnlyList<CharacterChoice>>> _search;
    private readonly Func<MessageEditorViewModel, Task<bool>> _submit;
    private string _searchText = string.Empty;
    private string? _searchMessage;
    private CharacterChoice? _foundCharacter;
    private CharacterChoice? _sender;
    private CharacterChoice? _selectedRecipient;
    private string _messageTitle = string.Empty;
    private string _body = string.Empty;

    /// <param name="limits">The most recipients, title characters, and text characters the API accepts.</param>
    /// <param name="replyToMessageId">The message this one replies to; <see langword="null"/> for a new message.</param>
    public MessageEditorViewModel(
        IManagementLocalizer localizer,
        string locale,
        string titleKey,
        MessageLimits limits,
        Guid? replyToMessageId,
        Func<string, Task<IReadOnlyList<CharacterChoice>>> search,
        Func<MessageEditorViewModel, Task<bool>> submit)
        : base(localizer, locale, titleKey)
    {
        Limits = limits;
        ReplyToMessageId = replyToMessageId;
        _search = search;
        _submit = submit;
        SearchCommand = new AsyncCommand(SearchAsync, HandleSearchFailure, () => !string.IsNullOrWhiteSpace(SearchText));
        SetSenderCommand = new DelegateCommand(() => Sender = FoundCharacter, () => FoundCharacter is not null);
        AddRecipientCommand = new DelegateCommand(AddRecipient,
            () => FoundCharacter is { } found && Recipients.Count < Limits.MaximumRecipients &&
                  Recipients.All(recipient => recipient.CharacterId != found.CharacterId));
        RemoveRecipientCommand = new DelegateCommand(RemoveRecipient, () => SelectedRecipient is not null);
        Recipients.CollectionChanged += (_, _) =>
        {
            OnPropertyChanged(nameof(RecipientCountText));
            AddRecipientCommand.NotifyCanExecuteChanged();
        };
    }

    /// <summary>Chosen when the dialog opens and sent with every try.</summary>
    public Guid MessageId { get; } = Guid.NewGuid();

    public Guid? ReplyToMessageId { get; }

    public MessageLimits Limits { get; }

    public AsyncCommand SearchCommand { get; }

    public DelegateCommand SetSenderCommand { get; }

    public DelegateCommand AddRecipientCommand { get; }

    public DelegateCommand RemoveRecipientCommand { get; }

    public ObservableCollection<CharacterChoice> FoundCharacters { get; } = [];

    public ObservableCollection<CharacterChoice> Recipients { get; } = [];

    public int MaximumTitleLength => Limits.MaximumTitleLength;

    public int MaximumBodyLength => Limits.MaximumBodyLength;

    public string SearchLabel => Translate("roster.search");

    public string SearchButtonLabel => Translate("players.search");

    public string FoundCharactersLabel => Translate("messages.findCharacter");

    public string SetSenderLabel => Translate("messages.setSender");

    public string AddRecipientLabel => Translate("messages.addRecipient");

    public string RemoveRecipientLabel => Translate("messages.removeRecipient");

    public string SenderLabel => Translate("messages.sender");

    public string RecipientsLabel => Translate("messages.recipients");

    public string MessageTitleLabel => Translate("messages.title");

    public string BodyLabel => Translate("messages.body");

    public string SenderText => Sender?.DisplayName ?? Translate("messages.noSender");

    public string RecipientCountText =>
        string.Format(CultureInfo.CurrentCulture, Translate("messages.recipientLimit"), Recipients.Count, Limits.MaximumRecipients);

    public string SearchText
    {
        get => _searchText;
        set
        {
            if (_searchText == value)
            {
                return;
            }

            _searchText = value;
            OnPropertyChanged();
            SearchCommand.NotifyCanExecuteChanged();
        }
    }

    public string? SearchMessage
    {
        get => _searchMessage;
        private set
        {
            if (_searchMessage == value)
            {
                return;
            }

            _searchMessage = value;
            OnPropertyChanged();
            OnPropertyChanged(nameof(HasSearchMessage));
        }
    }

    public bool HasSearchMessage => !string.IsNullOrEmpty(SearchMessage);

    public CharacterChoice? FoundCharacter
    {
        get => _foundCharacter;
        set
        {
            if (_foundCharacter == value)
            {
                return;
            }

            _foundCharacter = value;
            OnPropertyChanged();
            SetSenderCommand.NotifyCanExecuteChanged();
            AddRecipientCommand.NotifyCanExecuteChanged();
        }
    }

    public CharacterChoice? Sender
    {
        get => _sender;
        set
        {
            if (_sender == value)
            {
                return;
            }

            _sender = value;
            OnPropertyChanged();
            OnPropertyChanged(nameof(SenderText));
        }
    }

    public CharacterChoice? SelectedRecipient
    {
        get => _selectedRecipient;
        set
        {
            if (_selectedRecipient == value)
            {
                return;
            }

            _selectedRecipient = value;
            OnPropertyChanged();
            RemoveRecipientCommand.NotifyCanExecuteChanged();
        }
    }

    public string MessageTitle
    {
        get => _messageTitle;
        set
        {
            if (_messageTitle == value)
            {
                return;
            }

            _messageTitle = value;
            OnPropertyChanged();
        }
    }

    /// <summary>The text exactly as typed; it is sent unchanged.</summary>
    public string Body
    {
        get => _body;
        set
        {
            if (_body == value)
            {
                return;
            }

            _body = value;
            OnPropertyChanged();
        }
    }

    /// <summary>Adds a recipient unless it is already listed or the list is full.</summary>
    public void AddRecipient(CharacterChoice recipient)
    {
        if (Recipients.Count < Limits.MaximumRecipients && Recipients.All(listed => listed.CharacterId != recipient.CharacterId))
        {
            Recipients.Add(recipient);
        }
    }

    internal JsonElement CreateBody() =>
        JsonSerializer.SerializeToElement(
            new
            {
                messageId = MessageId,
                senderId = Sender?.CharacterId,
                recipientIds = Recipients.Select(recipient => recipient.CharacterId).ToArray(),
                title = MessageTitle,
                body = Body,
                replyToMessageId = ReplyToMessageId
            },
            JsonOptions);

    private protected override async Task<bool> SubmitAsync()
    {
        if (Sender is null)
        {
            ErrorMessage = Translate("messages.senderRequired");
            return false;
        }

        if (Recipients.Count == 0)
        {
            ErrorMessage = Translate("errors.noRecipients");
            return false;
        }

        return await _submit(this);
    }

    private void AddRecipient()
    {
        if (FoundCharacter is { } found)
        {
            AddRecipient(found);
        }
    }

    private void RemoveRecipient()
    {
        if (SelectedRecipient is { } selected)
        {
            Recipients.Remove(selected);
            SelectedRecipient = null;
        }
    }

    private async Task SearchAsync()
    {
        SearchMessage = null;
        var found = await _search(SearchText);
        FoundCharacters.Clear();
        foreach (var character in found)
        {
            FoundCharacters.Add(character);
        }

        FoundCharacter = FoundCharacters.FirstOrDefault();
        SearchMessage = FoundCharacters.Count == 0 ? Translate("common.noResults") : null;
    }

    private void HandleSearchFailure(Exception exception)
    {
        Trace.TraceError("Character search failed: {0}", exception);
        SearchMessage = Translate("common.error");
    }
}
