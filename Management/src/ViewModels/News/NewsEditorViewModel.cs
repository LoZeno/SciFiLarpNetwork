using System.Collections.ObjectModel;
using System.Diagnostics;
using System.Globalization;
using System.Text.Json;
using SciFiLarpNetwork.Common.NewsMarkdown;
using SciFiLarpNetwork.Management.Localization;
using SciFiLarpNetwork.Management.ViewModels.LarpEvents;
using SciFiLarpNetwork.Management.ViewModels.News.Preview;

namespace SciFiLarpNetwork.Management.ViewModels.News;

/// <summary>
/// The story dialog, for a new story or an edit: network, author, title, start and end in the computer's time zone,
/// hacking difficulty, and a Markdown text with a toolbar and a preview as players will see it. A new story's
/// identifier is chosen once when the dialog opens, so saving again after a failure never creates it twice.
/// </summary>
public sealed class NewsEditorViewModel : CatalogEditorViewModel
{
    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web);
    private readonly Func<string, Task<IReadOnlyList<CharacterChoice>>> _search;
    private readonly Func<Task<NewsMediaChoice?>> _chooseMedia;
    private readonly Func<IReadOnlyCollection<Guid>, Task<bool>> _loadPictures;
    private readonly Func<NewsEditorViewModel, Task<bool>> _submit;
    private readonly NewsPreviewBuilder _previewBuilder;
    private readonly AsyncCommand _loadPreviewPicturesCommand;
    private string _searchText = string.Empty;
    private string? _searchMessage;
    private CharacterChoice? _foundCharacter;
    private CharacterChoice? _author;
    private NetworkChoice? _network;
    private string _storyTitle = string.Empty;
    private string _body = string.Empty;
    private int _selectionStart;
    private int _selectionEnd;
    private DateTime? _startDate;
    private TimeSpan? _startTime;
    private DateTime? _endDate;
    private TimeSpan? _endTime;
    private decimal? _hackingDifficulty;
    private IReadOnlyList<NewsPreviewBlock> _preview = [];

    /// <param name="version">The story version an edit is based on; <see langword="null"/> for a new story.</param>
    /// <param name="networks">The networks a story can be on: the enabled ones, and the story's own network.</param>
    internal NewsEditorViewModel(
        IManagementLocalizer localizer,
        string locale,
        string titleKey,
        NewsEditorLimits limits,
        TimeZoneInfo timeZone,
        IReadOnlyList<NetworkChoice> networks,
        long? version,
        Func<string, Task<IReadOnlyList<CharacterChoice>>> search,
        Func<Task<NewsMediaChoice?>> chooseMedia,
        Func<Guid, NewsMediaItem?> findMedia,
        Func<IReadOnlyCollection<Guid>, Task<bool>> loadPictures,
        Func<NewsEditorViewModel, Task<bool>> submit)
        : base(localizer, locale, titleKey)
    {
        Limits = limits;
        TimeZone = timeZone;
        Version = version;
        _search = search;
        _chooseMedia = chooseMedia;
        _loadPictures = loadPictures;
        _submit = submit;
        _previewBuilder = new NewsPreviewBuilder(findMedia, Translate);
        _hackingDifficulty = limits.MinimumHackingDifficulty;
        foreach (var network in networks)
        {
            Networks.Add(network);
        }

        SearchCommand = new AsyncCommand(SearchAsync, HandleSearchFailure, () => !string.IsNullOrWhiteSpace(SearchText));
        SetAuthorCommand = new DelegateCommand(() => Author = FoundCharacter, () => FoundCharacter is not null);
        BoldCommand = new DelegateCommand(() => Apply(NewsBodyEdits.Wrap(Body, SelectionStart, SelectionEnd, "**")));
        ItalicCommand = new DelegateCommand(() => Apply(NewsBodyEdits.Wrap(Body, SelectionStart, SelectionEnd, "*")));
        HeadingCommand = new DelegateCommand(() => Apply(NewsBodyEdits.PrefixLines(Body, SelectionStart, SelectionEnd, _ => "## ")));
        BulletedListCommand = new DelegateCommand(() => Apply(NewsBodyEdits.PrefixLines(Body, SelectionStart, SelectionEnd, _ => "- ")));
        NumberedListCommand = new DelegateCommand(() =>
            Apply(NewsBodyEdits.PrefixLines(Body, SelectionStart, SelectionEnd, number => $"{number}. ")));
        InsertMediaCommand = new AsyncCommand(InsertMediaAsync, HandleUnexpectedEditorFailure);
        _loadPreviewPicturesCommand = new AsyncCommand(LoadPreviewPicturesAsync, HandleUnexpectedEditorFailure);
    }

    /// <summary>Chosen when the dialog opens and sent with every try of a new story.</summary>
    public Guid StoryId { get; } = Guid.NewGuid();

    /// <summary>The version an edit is based on; <see langword="null"/> for a new story.</summary>
    public long? Version { get; private set; }

    public bool IsEdit => Version is not null;

    public NewsEditorLimits Limits { get; }

    public TimeZoneInfo TimeZone { get; }

    public AsyncCommand SearchCommand { get; }

    public DelegateCommand SetAuthorCommand { get; }

    public DelegateCommand BoldCommand { get; }

    public DelegateCommand ItalicCommand { get; }

    public DelegateCommand HeadingCommand { get; }

    public DelegateCommand BulletedListCommand { get; }

    public DelegateCommand NumberedListCommand { get; }

    public AsyncCommand InsertMediaCommand { get; }

    public ObservableCollection<CharacterChoice> FoundCharacters { get; } = [];

    public ObservableCollection<NetworkChoice> Networks { get; } = [];

    public IReadOnlyList<NewsPreviewBlock> Preview
    {
        get => _preview;
        private set
        {
            _preview = value;
            OnPropertyChanged();
        }
    }

    public int MaximumTitleLength => Limits.MaximumTitleLength;

    public int MaximumBodyLength => Limits.MaximumBodyLength;

    public decimal MinimumHackingDifficulty => Limits.MinimumHackingDifficulty;

    public string NetworkLabel => Translate("news.network");

    public string AuthorLabel => Translate("news.author");

    public string SearchLabel => Translate("roster.search");

    public string SearchButtonLabel => Translate("players.search");

    public string SetAuthorLabel => Translate("news.setAuthor");

    public string StoryTitleLabel => Translate("news.title");

    public string BodyLabel => Translate("news.body");

    public string StartLabel => Translate("news.startsAt");

    public string EndLabel => Translate("news.endsAt");

    public string HackingDifficultyLabel => Translate("news.hackingDifficulty");

    public string PreviewLabel => Translate("news.preview");

    public string BoldLabel => Translate("news.bold");

    public string ItalicLabel => Translate("news.italic");

    public string HeadingLabel => Translate("news.heading");

    public string BulletedListLabel => Translate("news.bulletedList");

    public string NumberedListLabel => Translate("news.numberedList");

    public string InsertMediaLabel => Translate("news.insertMedia");

    public string TimeZoneText => string.Format(CultureInfo.CurrentCulture, Translate("news.timeZone"), TimeZone.DisplayName);

    public string AuthorText => Author?.DisplayName ?? Translate("news.noAuthor");

    public string MediaCountText => string.Format(
        CultureInfo.CurrentCulture,
        Translate("news.mediaCount"),
        NewsBodyMedia.Scan(Body).Embeds.Count,
        Limits.MaximumMediaPerStory);

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
            SetAuthorCommand.NotifyCanExecuteChanged();
        }
    }

    public CharacterChoice? Author
    {
        get => _author;
        set
        {
            if (_author == value)
            {
                return;
            }

            _author = value;
            OnPropertyChanged();
            OnPropertyChanged(nameof(AuthorText));
        }
    }

    public NetworkChoice? Network
    {
        get => _network;
        set
        {
            if (_network == value)
            {
                return;
            }

            _network = value;
            OnPropertyChanged();
        }
    }

    public string StoryTitle
    {
        get => _storyTitle;
        set
        {
            if (_storyTitle == value)
            {
                return;
            }

            _storyTitle = value;
            OnPropertyChanged();
        }
    }

    /// <summary>The Markdown text exactly as typed; it is sent unchanged, and the preview follows every change.</summary>
    public string Body
    {
        get => _body;
        set
        {
            if (_body == value)
            {
                return;
            }

            _body = value ?? string.Empty;
            OnPropertyChanged();
            OnPropertyChanged(nameof(MediaCountText));
            RefreshPreview();
            _loadPreviewPicturesCommand.Execute(null);
        }
    }

    public int SelectionStart
    {
        get => _selectionStart;
        set
        {
            if (_selectionStart == value)
            {
                return;
            }

            _selectionStart = value;
            OnPropertyChanged();
        }
    }

    public int SelectionEnd
    {
        get => _selectionEnd;
        set
        {
            if (_selectionEnd == value)
            {
                return;
            }

            _selectionEnd = value;
            OnPropertyChanged();
        }
    }

    public DateTime? StartDate
    {
        get => _startDate;
        set => SetValue(ref _startDate, value);
    }

    public TimeSpan? StartTime
    {
        get => _startTime;
        set => SetValue(ref _startTime, value);
    }

    public DateTime? EndDate
    {
        get => _endDate;
        set => SetValue(ref _endDate, value);
    }

    public TimeSpan? EndTime
    {
        get => _endTime;
        set => SetValue(ref _endTime, value);
    }

    public decimal? HackingDifficulty
    {
        get => _hackingDifficulty;
        set => SetValue(ref _hackingDifficulty, value);
    }

    /// <summary>Sets the start in the editor's time zone, to the minute.</summary>
    public void SetStart(DateTimeOffset instant)
    {
        var local = NewsLocalTimes.ToLocal(instant, TimeZone);
        StartDate = local.Date;
        StartTime = local.TimeOfDay;
    }

    /// <summary>Sets the end in the editor's time zone, to the minute.</summary>
    public void SetEnd(DateTimeOffset instant)
    {
        var local = NewsLocalTimes.ToLocal(instant, TimeZone);
        EndDate = local.Date;
        EndTime = local.TimeOfDay;
    }

    /// <summary>
    /// After someone else saved the story, the typed values are kept and the next save is based on the version now
    /// stored, so saving again is a deliberate choice to replace that change.
    /// </summary>
    internal void BaseOn(long version) => Version = version;

    internal JsonElement CreateBody() =>
        IsEdit
            ? JsonSerializer.SerializeToElement(new
            {
                version = Version,
                networkId = Network?.NetworkId,
                authorId = Author?.CharacterId,
                title = StoryTitle,
                body = Body,
                startsAt = StartsAt,
                endsAt = EndsAt,
                hackingDifficulty = (int)(HackingDifficulty ?? 0)
            }, JsonOptions)
            : JsonSerializer.SerializeToElement(new
            {
                storyId = StoryId,
                networkId = Network?.NetworkId,
                authorId = Author?.CharacterId,
                title = StoryTitle,
                body = Body,
                startsAt = StartsAt,
                endsAt = EndsAt,
                hackingDifficulty = (int)(HackingDifficulty ?? 0)
            }, JsonOptions);

    private DateTimeOffset? StartsAt => NewsLocalTimes.ToInstant(StartDate, StartTime, TimeZone);

    private DateTimeOffset? EndsAt => NewsLocalTimes.ToInstant(EndDate, EndTime, TimeZone);

    private protected override async Task<bool> SubmitAsync()
    {
        ErrorMessage = Network is null ? Translate("news.networkRequired")
            : Author is null ? Translate("news.authorRequired")
            : StartDate is null || StartTime is null || EndDate is null || EndTime is null ? Translate("news.timesRequired")
            : StartsAt is null || EndsAt is null ? Translate("news.invalidLocalTime")
            : HackingDifficulty is not { } difficulty || difficulty < Limits.MinimumHackingDifficulty || difficulty != decimal.Truncate(difficulty)
                ? Translate("errors.invalidHackingDifficulty")
                : null;
        return ErrorMessage is null && await _submit(this);
    }

    private void Apply(NewsBodyEdit edit)
    {
        Body = edit.Text;
        SelectionStart = edit.SelectionStart;
        SelectionEnd = edit.SelectionEnd;
    }

    private async Task InsertMediaAsync()
    {
        if (await _chooseMedia() is { } choice)
        {
            Apply(NewsBodyEdits.InsertBlock(Body, SelectionStart, SelectionEnd, NewsBodyMedia.Write(choice.Embed)));
        }
    }

    private void RefreshPreview() => Preview = _previewBuilder.Build(Body);

    /// <summary>Downloads the pictures the preview shows without them, then draws the preview again.</summary>
    private async Task LoadPreviewPicturesAsync()
    {
        Guid[] missing = [.. Preview.OfType<NewsPreviewPicture>().Where(picture => picture.Content is null).Select(picture => picture.MediaId)];
        if (missing.Length > 0 && await _loadPictures(missing))
        {
            RefreshPreview();
        }
    }

    private void SetValue<T>(ref T field, T value, [System.Runtime.CompilerServices.CallerMemberName] string? name = null)
    {
        if (EqualityComparer<T>.Default.Equals(field, value))
        {
            return;
        }

        field = value;
        OnPropertyChanged(name);
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

    private void HandleUnexpectedEditorFailure(Exception exception)
    {
        Trace.TraceError("Unhandled story editor exception: {0}", exception);
        ErrorMessage = Translate("common.error");
    }
}
