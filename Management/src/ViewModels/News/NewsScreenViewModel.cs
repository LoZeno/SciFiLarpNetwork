using System.Collections.ObjectModel;
using System.Globalization;
using SciFiLarpNetwork.Common.Siren;
using SciFiLarpNetwork.Management.Api;
using SciFiLarpNetwork.Management.Api.Networks;
using SciFiLarpNetwork.Management.Api.News;
using SciFiLarpNetwork.Management.Api.Players;
using SciFiLarpNetwork.Management.Api.Skills;
using SciFiLarpNetwork.Management.Localization;
using SciFiLarpNetwork.Management.ViewModels.LarpEvents;
using SciFiLarpNetwork.Management.ViewModels.News.Preview;

namespace SciFiLarpNetwork.Management.ViewModels.News;

/// <summary>
/// The news screen: one network's stories, those active at a chosen date and time or all of them, in pages, and the
/// selected story as players will see it. Times are shown and entered in the computer's time zone. Creating and
/// editing stories and changing the media library follow the actions the API advertises to administrators.
/// </summary>
public sealed class NewsScreenViewModel : ScreenViewModel
{
    private const string ConcurrentChangeCode = "errors.concurrentChange";
    private readonly INewsDialogService _dialogs;
    private readonly string _newsHref;
    private readonly NewsMediaCache _cache;
    private readonly AsyncCommand _reloadCommand;
    private readonly AsyncCommand _openStoryCommand;
    private readonly AsyncCommand _filterCommand;
    private SirenEntity? _news;
    private SirenEntity? _story;
    private SirenEntity? _saved;
    private NewsCollectionProperties? _page;
    private NewsStoryDetailsProperties? _details;
    private NewsStoryListItem? _selectedStory;
    private NetworkChoice? _selectedNetwork;
    private bool _showAll;
    private DateTime? _activeAtDate;
    private TimeSpan? _activeAtTime;
    private int _listLoadGeneration;
    private int _detailLoadGeneration;

    public NewsScreenViewModel(
        IManagementApiClient apiClient,
        IManagementLocalizer localizer,
        INewsDialogService dialogs,
        string apiBaseUrl,
        string locale,
        string newsHref,
        TimeZoneInfo timeZone,
        TimeProvider timeProvider)
        : base(apiClient, localizer, apiBaseUrl, locale)
    {
        _dialogs = dialogs;
        _newsHref = newsHref;
        TimeZone = timeZone;
        TimeProvider = timeProvider;
        _cache = new NewsMediaCache(apiClient, apiBaseUrl);
        var now = NewsLocalTimes.ToLocal(timeProvider.GetUtcNow(), timeZone);
        _activeAtDate = now.Date;
        _activeAtTime = now.TimeOfDay;
        _reloadCommand = new AsyncCommand(LoadAsync, HandleUnexpectedFailure);
        _openStoryCommand = new AsyncCommand(() => RunAsync(() => OpenAsync(SelectedStory?.Href)), HandleUnexpectedFailure);
        _filterCommand = new AsyncCommand(() => RunAsync(() => LoadStoriesAsync(null)), HandleUnexpectedFailure);
        ApplyFilterCommand = new AsyncCommand(() => RunAsync(() => LoadStoriesAsync(null)), HandleUnexpectedFailure,
            () => !IsBusy && SelectedNetwork is not null);
        PreviousStoriesCommand = new AsyncCommand(
            () => RunAsync(() => LoadStoriesAsync(_news?.FindLinkHref(SirenRelations.Previous))),
            HandleUnexpectedFailure,
            () => !IsBusy && _news?.FindLinkHref(SirenRelations.Previous) is not null);
        NextStoriesCommand = new AsyncCommand(
            () => RunAsync(() => LoadStoriesAsync(_news?.FindLinkHref(SirenRelations.Next))),
            HandleUnexpectedFailure,
            () => !IsBusy && _news?.FindLinkHref(SirenRelations.Next) is not null);
        NewStoryCommand = new AsyncCommand(() => RunAsync(NewStoryAsync), HandleUnexpectedFailure,
            () => !IsBusy && _news?.FindAction("create-news-story") is not null);
        EditStoryCommand = new AsyncCommand(() => RunAsync(EditStoryAsync), HandleUnexpectedFailure,
            () => !IsBusy && _story?.FindAction("edit-news-story") is not null);
        OpenMediaLibraryCommand = new AsyncCommand(() => RunAsync(OpenMediaLibraryAsync), HandleUnexpectedFailure,
            () => !IsBusy && _news?.FindLinkHref(SirenRelations.NewsMedia) is not null);
    }

    public TimeZoneInfo TimeZone { get; }

    public TimeProvider TimeProvider { get; }

    public AsyncCommand ApplyFilterCommand { get; }

    public AsyncCommand PreviousStoriesCommand { get; }

    public AsyncCommand NextStoriesCommand { get; }

    public AsyncCommand NewStoryCommand { get; }

    public AsyncCommand EditStoryCommand { get; }

    public AsyncCommand OpenMediaLibraryCommand { get; }

    public ObservableCollection<NetworkChoice> Networks { get; } = [];

    public ObservableCollection<NewsStoryListItem> Stories { get; } = [];

    /// <summary>Choosing a network lists its stories.</summary>
    public NetworkChoice? SelectedNetwork
    {
        get => _selectedNetwork;
        set
        {
            if (_selectedNetwork == value)
            {
                return;
            }

            _selectedNetwork = value;
            OnPropertyChanged();
            NotifyCommandsChanged();
            if (value is not null)
            {
                _filterCommand.Execute(null);
            }
        }
    }

    /// <summary>Whether every story is listed, including scheduled and expired ones, instead of those active at a time.</summary>
    public bool ShowAll
    {
        get => _showAll;
        set
        {
            if (_showAll == value)
            {
                return;
            }

            _showAll = value;
            OnPropertyChanged();
            OnPropertyChanged(nameof(ShowActiveAt));
            if (SelectedNetwork is not null)
            {
                _filterCommand.Execute(null);
            }
        }
    }

    public bool ShowActiveAt
    {
        get => !ShowAll;
        set => ShowAll = !value;
    }

    public DateTime? ActiveAtDate
    {
        get => _activeAtDate;
        set
        {
            if (_activeAtDate == value)
            {
                return;
            }

            _activeAtDate = value;
            OnPropertyChanged();
        }
    }

    public TimeSpan? ActiveAtTime
    {
        get => _activeAtTime;
        set
        {
            if (_activeAtTime == value)
            {
                return;
            }

            _activeAtTime = value;
            OnPropertyChanged();
        }
    }

    public NewsStoryListItem? SelectedStory
    {
        get => _selectedStory;
        set
        {
            if (_selectedStory == value)
            {
                return;
            }

            _selectedStory = value;
            OnPropertyChanged();
            _openStoryCommand.Execute(null);
        }
    }

    public string StoryPage { get; private set; } = string.Empty;

    public bool HasDetails => _details is not null;

    public string DetailTitle => _details?.Title ?? string.Empty;

    public string DetailAuthor => _details is { } details
        ? string.Format(CultureInfo.CurrentCulture, Translate("news.byAuthor"), DescribeAuthor(details.Author))
        : string.Empty;

    public string DetailNetwork => _details is { } details
        ? string.Format(CultureInfo.CurrentCulture, Translate("news.onNetwork"), DescribeNetwork(details.Network))
        : string.Empty;

    public string DetailTimes => _details is { } details
        ? string.Format(CultureInfo.CurrentCulture, Translate("news.fromTo"), FormatTime(details.StartsAtUtc), FormatTime(details.EndsAtUtc))
        : string.Empty;

    public string DetailStatus => _details is { } details ? StatusText(details.Status) : string.Empty;

    public string DetailHackingDifficulty => _details is { } details
        ? string.Format(CultureInfo.CurrentCulture, Translate("news.difficultyValue"), details.HackingDifficulty)
        : string.Empty;

    public string DetailSaved => _details is { } details
        ? string.Format(CultureInfo.CurrentCulture, Translate("news.savedBy"), details.SavedBy ?? "?", FormatTime(details.SavedAtUtc))
        : string.Empty;

    /// <summary>The story text as players will see it.</summary>
    public IReadOnlyList<NewsPreviewBlock> DetailPreview { get; private set; } = [];

    public string NewsHeading => Translate("api.news");

    public string DetailsHeading => Translate("news.details");

    public string NetworkLabel => Translate("news.network");

    public string ActiveAtLabel => Translate("news.activeAtMode");

    public string AllLabel => Translate("news.allMode");

    public string ApplyFilterLabel => Translate("news.show");

    public string PreviousLabel => Translate("players.previousPage");

    public string NextLabel => Translate("players.nextPage");

    public string NewLabel => Translate("news.new");

    public string EditLabel => Translate("news.edit");

    public string MediaLibraryLabel => Translate("news.mediaLibrary");

    public string TimeZoneText => string.Format(CultureInfo.CurrentCulture, Translate("news.timeZone"), TimeZone.DisplayName);

    public string? StoriesPlaceholder => SelectedNetwork is null && _page is not null ? Translate("news.chooseNetwork")
        : _page is not null && Stories.Count == 0 ? Translate(ShowAll ? "news.noStories" : "news.noActiveStories")
        : null;

    public bool HasStoriesPlaceholder => StoriesPlaceholder is not null;

    public string? DetailsPlaceholder => _details is null ? Translate("news.selectStory") : null;

    public bool HasDetailsPlaceholder => DetailsPlaceholder is not null;

    public override Task LoadAsync() => RunAsync(async () =>
    {
        _news = await GetAsync(_newsHref);
        _page = SirenProperties.Read<NewsCollectionProperties>(_news.Properties);
        IsLoaded = true;
        CanWrite = _news.FindAction("create-news-story") is not null;
        await LoadNetworksAsync();
        await LoadStoriesAsync(null);
    });

    private protected override void OnLocaleChanged()
    {
        foreach (var name in new[]
                 {
                     nameof(NewsHeading), nameof(DetailsHeading), nameof(NetworkLabel), nameof(ActiveAtLabel), nameof(AllLabel),
                     nameof(ApplyFilterLabel), nameof(PreviousLabel), nameof(NextLabel), nameof(NewLabel), nameof(EditLabel),
                     nameof(MediaLibraryLabel), nameof(TimeZoneText)
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
                     ApplyFilterCommand, PreviousStoriesCommand, NextStoriesCommand, NewStoryCommand, EditStoryCommand,
                     OpenMediaLibraryCommand
                 })
        {
            command.NotifyCanExecuteChanged();
        }
    }

    /// <summary>
    /// Lists every network, disabled ones included, so their stories can still be read. The network already chosen
    /// stays chosen; otherwise the default network, or else the first enabled one, is chosen.
    /// </summary>
    private async Task LoadNetworksAsync()
    {
        var catalog = await GetAsync(_news?.FindLinkHref(SirenRelations.Networks) ?? throw MissingAction());
        var all = await GetAsync(catalog.FindLinkHref(SirenRelations.WithDisabled) ?? throw MissingAction());
        var networks = (all.Entities ?? []).Select(entity => SirenProperties.Read<NetworkProperties>(entity.Properties)).ToArray();
        var chosenId = _selectedNetwork?.NetworkId
                       ?? networks.FirstOrDefault(network => network.IsDefault)?.NetworkId
                       ?? networks.FirstOrDefault(network => network.IsEnabled)?.NetworkId;
        ReplaceAll(Networks, networks.Select(network => new NetworkChoice(
            network.NetworkId,
            network.IsEnabled ? network.Name : $"{network.Name} {Translate("networks.disabledTag")}",
            network.IsEnabled)));
        _selectedNetwork = Networks.FirstOrDefault(network => network.NetworkId == chosenId);
        OnPropertyChanged(nameof(SelectedNetwork));
    }

    private async Task LoadStoriesAsync(string? href)
    {
        var generation = ++_listLoadGeneration;
        SirenEntity collection;
        if (href is not null)
        {
            collection = await GetAsync(href);
        }
        else if (SelectedNetwork is { } network)
        {
            var activeAt = ShowAll ? null : ActiveAt();
            if (!ShowAll && activeAt is null)
            {
                StatusMessage = Translate("news.invalidLocalTime");
                return;
            }

            var filter = _news?.FindAction("filter-news") ?? throw MissingAction();
            collection = await GetAsync(filter.ToQueryHref(new Dictionary<string, string?>
            {
                ["networkId"] = network.NetworkId.ToString("D"),
                ["activeAt"] = activeAt?.ToString("O", CultureInfo.InvariantCulture)
            }));
        }
        else
        {
            collection = await GetAsync(_newsHref);
        }

        if (generation != _listLoadGeneration)
        {
            return;
        }

        _news = collection;
        _page = SirenProperties.Read<NewsCollectionProperties>(collection.Properties);
        StoryPage = string.Format(CultureInfo.CurrentCulture, Translate("players.page"), _page.PageNumber, _page.PageCount, _page.TotalCount);
        OnPropertyChanged(nameof(StoryPage));
        var selectedId = SelectedStory?.StoryId;
        ReplaceAll(Stories, (collection.Entities ?? []).Select(ToListItem));
        _selectedStory = Stories.FirstOrDefault(item => item.StoryId == selectedId);
        OnPropertyChanged(nameof(SelectedStory));
        OnPropertyChanged(nameof(StoriesPlaceholder));
        OnPropertyChanged(nameof(HasStoriesPlaceholder));
        NotifyCommandsChanged();
    }

    /// <summary>
    /// The chosen date and time as an instant; with no time, the start of the day. <see langword="null"/> when no
    /// date is chosen or the time does not exist in this time zone.
    /// </summary>
    private DateTimeOffset? ActiveAt() => NewsLocalTimes.ToInstant(ActiveAtDate, ActiveAtTime ?? TimeSpan.Zero, TimeZone);

    private async Task OpenAsync(string? href)
    {
        var generation = ++_detailLoadGeneration;
        var resource = href is null ? null : await GetAsync(href);
        if (generation != _detailLoadGeneration)
        {
            return;
        }

        await ShowDetailsAsync(resource);
    }

    private async Task ShowDetailsAsync(SirenEntity? resource)
    {
        _story = resource;
        _details = resource is null ? null : SirenProperties.Read<NewsStoryDetailsProperties>(resource.Properties);
        if (resource is not null)
        {
            _cache.Remember((resource.Entities ?? []).Select(NewsMediaMappings.ToMediaItem));
        }

        RefreshDetails();
        NotifyCommandsChanged();
        if (_details is { } details && await _cache.LoadPicturesAsync(details.Media.Select(media => media.MediaId), Locale))
        {
            RefreshDetails();
        }
    }

    private void RefreshDetails()
    {
        DetailPreview = _details is { } details ? PreviewBuilder().Build(details.Body) : [];
        foreach (var name in new[]
                 {
                     nameof(HasDetails), nameof(DetailTitle), nameof(DetailAuthor), nameof(DetailNetwork), nameof(DetailTimes),
                     nameof(DetailStatus), nameof(DetailHackingDifficulty), nameof(DetailSaved), nameof(DetailPreview),
                     nameof(DetailsPlaceholder), nameof(HasDetailsPlaceholder)
                 })
        {
            OnPropertyChanged(name);
        }
    }

    private NewsPreviewBuilder PreviewBuilder() => new(_cache.Find, Translate);

    private async Task NewStoryAsync()
    {
        var action = _news?.FindAction("create-news-story") ?? throw MissingAction();
        var editor = CreateEditor("news.newStory", null, action, null);
        editor.Network = editor.Networks.FirstOrDefault(network => network.NetworkId == SelectedNetwork?.NetworkId && network.IsEnabled);
        var start = TimeProvider.GetUtcNow();
        editor.SetStart(start);
        editor.SetEnd(start.AddDays(1));
        await _dialogs.ShowStoryEditorAsync(editor);
        await ShowSavedAsync(editor);
    }

    private async Task EditStoryAsync()
    {
        var action = _story?.FindAction("edit-news-story") ?? throw MissingAction();
        var details = _details ?? throw MissingAction();
        var editor = CreateEditor("news.editStory", details.Version, action, details.Network);
        editor.Network = editor.Networks.FirstOrDefault(network => network.NetworkId == details.Network.NetworkId);
        editor.Author = new CharacterChoice(details.Author.CharacterId, DescribeAuthor(details.Author));
        editor.StoryTitle = details.Title;
        editor.Body = details.Body;
        editor.SetStart(details.StartsAtUtc);
        editor.SetEnd(details.EndsAtUtc);
        editor.HackingDifficulty = details.HackingDifficulty;
        await _dialogs.ShowStoryEditorAsync(editor);
        await ShowSavedAsync(editor);
    }

    /// <summary>The editor offers the enabled networks, and an edited story's own network even when it is disabled.</summary>
    private NewsEditorViewModel CreateEditor(string titleKey, long? version, SirenAction action, NewsNetworkProperties? current)
    {
        var page = _page ?? throw MissingAction();
        var networks = Networks.Where(network => network.IsEnabled || network.NetworkId == current?.NetworkId).ToArray();
        return new NewsEditorViewModel(
            Localizer,
            Locale,
            titleKey,
            new NewsEditorLimits(page.MaximumTitleLength, page.MaximumBodyLength, page.MaximumMediaPerStory, page.MinimumHackingDifficulty),
            TimeZone,
            networks,
            version,
            SearchCharactersAsync,
            ChooseMediaAsync,
            _cache.Find,
            ids => _cache.LoadPicturesAsync(ids, Locale),
            editor => SaveAsync(editor, action));
    }

    /// <summary>
    /// Saves the story. When someone else saved it first, the dialog keeps the typed values, says so, and bases the
    /// next save on the version now stored.
    /// </summary>
    private async Task<bool> SaveAsync(NewsEditorViewModel editor, SirenAction action)
    {
        try
        {
            _saved = await SubmitAsync(action, editor.CreateBody());
            return true;
        }
        catch (Exception failure) when (IsExpectedFailure(failure))
        {
            editor.ErrorMessage = Report(failure);
            if (failure is ManagementApiException { ErrorCode: ConcurrentChangeCode } && _story?.FindLinkHref("self") is { } self)
            {
                var latest = SirenProperties.Read<NewsStoryDetailsProperties>((await GetAsync(self)).Properties);
                editor.BaseOn(latest.Version);
            }

            return false;
        }
    }

    /// <summary>After a save, lists the story's network and shows the saved story from the save's answer.</summary>
    private async Task ShowSavedAsync(NewsEditorViewModel editor)
    {
        if (!editor.IsSaved || _saved is not { } saved)
        {
            return;
        }

        _saved = null;
        var details = SirenProperties.Read<NewsStoryDetailsProperties>(saved.Properties);
        _selectedNetwork = Networks.FirstOrDefault(network => network.NetworkId == details.Network.NetworkId) ?? _selectedNetwork;
        OnPropertyChanged(nameof(SelectedNetwork));
        await LoadStoriesAsync(null);
        _selectedStory = Stories.FirstOrDefault(item => item.StoryId == details.StoryId);
        OnPropertyChanged(nameof(SelectedStory));
        ++_detailLoadGeneration;
        await ShowDetailsAsync(saved);
        StatusMessage = Translate("actions.completed");
    }

    private async Task OpenMediaLibraryAsync()
    {
        var library = CreateLibrary(isChoosing: false);
        await library.LoadAsync();
        await _dialogs.ShowMediaLibraryAsync(library);
        RefreshDetails();
    }

    private async Task<NewsMediaChoice?> ChooseMediaAsync()
    {
        var library = CreateLibrary(isChoosing: true);
        await library.LoadAsync();
        await _dialogs.ShowMediaLibraryAsync(library);
        return library.Chosen;
    }

    private NewsMediaLibraryViewModel CreateLibrary(bool isChoosing) =>
        new(ApiClient, Localizer, _dialogs, _cache, ApiBaseUrl, Locale,
            _news?.FindLinkHref(SirenRelations.NewsMedia) ?? throw MissingAction(), isChoosing);

    private async Task<IReadOnlyList<CharacterChoice>> SearchCharactersAsync(string text)
    {
        var characters = await GetAsync(_news?.FindLinkHref(SirenRelations.Characters) ?? throw MissingAction());
        var search = characters.FindAction("search-characters") ?? throw MissingAction();
        var results = await GetAsync(search.ToQueryHref(new Dictionary<string, string?> { ["search"] = text }));
        return (results.Entities ?? [])
            .Select(entity => SirenProperties.Read<CharacterProperties>(entity.Properties))
            .Select(character => new CharacterChoice(
                character.CharacterId,
                $"#{character.Number} {character.Name} — {character.PlayerDisplayName}"))
            .ToArray();
    }

    private string DescribeAuthor(NewsCharacterProperties author) =>
        !author.IsKnown ? Translate("messages.unknownCharacter")
        : author.IsArchived ? $"#{author.Number} {author.Name} {Translate("characters.archivedTag")}"
        : $"#{author.Number} {author.Name}";

    private string DescribeNetwork(NewsNetworkProperties network) =>
        !network.IsKnown ? Translate("news.unknownNetwork")
        : network.IsEnabled ? network.Name ?? string.Empty
        : $"{network.Name} {Translate("networks.disabledTag")}";

    private string StatusText(string status) =>
        status.ToUpperInvariant() switch
        {
            "SCHEDULED" => Translate("news.status.scheduled"),
            "ACTIVE" => Translate("news.status.active"),
            "EXPIRED" => Translate("news.status.expired"),
            _ => status
        };

    private string FormatTime(DateTimeOffset value) =>
        NewsLocalTimes.ToLocal(value, TimeZone).ToString("g", CultureInfo.CurrentCulture);

    private NewsStoryListItem ToListItem(SirenSubEntity entity)
    {
        var story = SirenProperties.Read<NewsStorySummaryProperties>(entity.Properties);
        return new NewsStoryListItem(
            story.StoryId,
            entity.Href ?? throw MissingAction(),
            string.Format(
                CultureInfo.CurrentCulture,
                Translate("news.listEntry"),
                FormatTime(story.StartsAtUtc),
                FormatTime(story.EndsAtUtc),
                story.Title,
                DescribeAuthor(story.Author),
                StatusText(story.Status)));
    }
}
