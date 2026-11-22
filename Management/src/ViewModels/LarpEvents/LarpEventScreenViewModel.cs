using System.Collections.ObjectModel;
using System.Globalization;
using System.Text.Json;
using SciFiLarpNetwork.Common.Siren;
using SciFiLarpNetwork.Management.Api;
using SciFiLarpNetwork.Management.Api.LarpEvents;
using SciFiLarpNetwork.Management.Api.Players;
using SciFiLarpNetwork.Management.Api.Skills;
using SciFiLarpNetwork.Management.Localization;

namespace SciFiLarpNetwork.Management.ViewModels.LarpEvents;

/// <summary>
/// The events screen: a paged event list, the selected event's days, and the characters registered for the selected
/// day or for every day. Change commands follow the actions the API advertises, which it offers only to
/// administrators and only while the event is Open; a Closed event explains why nothing can change.
/// </summary>
public sealed class LarpEventScreenViewModel : ScreenViewModel
{
    private const string OpenStatus = "open";
    private const string PdfMediaType = "application/pdf";
    private const string CardsFileName = "participant-cards.pdf";
    private const string IsoDate = "yyyy-MM-dd";
    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web);
    private readonly ILarpEventDialogService _dialogs;
    private readonly string _eventsHref;
    private readonly AsyncCommand _reloadCommand;
    private readonly AsyncCommand _reloadEventCommand;
    private readonly AsyncCommand _reloadParticipantsCommand;
    private SirenEntity? _events;
    private SirenEntity? _event;
    private LarpEventProperties? _eventProperties;
    private LarpEventListItem? _selectedEvent;
    private DayListItem? _selectedDay;
    private ParticipantListItem? _selectedParticipant;
    private Dictionary<Guid, ParticipantProperties> _participantDetails = [];
    private bool _showArchived;
    private int _eventLoadGeneration;
    private int _participantLoadGeneration;

    public LarpEventScreenViewModel(
        IManagementApiClient apiClient,
        IManagementLocalizer localizer,
        ILarpEventDialogService dialogs,
        string apiBaseUrl,
        string locale,
        string eventsHref)
        : base(apiClient, localizer, apiBaseUrl, locale)
    {
        _dialogs = dialogs;
        _eventsHref = eventsHref;
        _reloadCommand = new AsyncCommand(LoadAsync, HandleUnexpectedFailure);
        _reloadEventCommand = new AsyncCommand(() => RunAsync(LoadSelectedEventAsync), HandleUnexpectedFailure);
        _reloadParticipantsCommand = new AsyncCommand(() => RunAsync(LoadParticipantsAsync), HandleUnexpectedFailure);
        PreviousEventsCommand = new AsyncCommand(
            () => RunAsync(() => LoadEventsCoreAsync(_events?.FindLinkHref(SirenRelations.Previous))),
            HandleUnexpectedFailure,
            () => !IsBusy && _events?.FindLinkHref(SirenRelations.Previous) is not null);
        NextEventsCommand = new AsyncCommand(
            () => RunAsync(() => LoadEventsCoreAsync(_events?.FindLinkHref(SirenRelations.Next))),
            HandleUnexpectedFailure,
            () => !IsBusy && _events?.FindLinkHref(SirenRelations.Next) is not null);
        NewEventCommand = new AsyncCommand(() => RunAsync(CreateEventAsync), HandleUnexpectedFailure,
            () => !IsBusy && _events?.FindAction("create-event") is not null);
        EditEventCommand = EventCommand(EditEventAsync, "update-event");
        CloseEventCommand = EventCommand(CloseEventAsync, "close-event");
        ReopenEventCommand = EventCommand(() => ConfirmEventActionAsync("reopen-event", "events.confirmReopen", "events.reopen"), "reopen-event");
        ArchiveEventCommand = EventCommand(() => ConfirmEventActionAsync("archive-event", "events.confirmArchive", "events.archive"), "archive-event");
        UnarchiveEventCommand = EventCommand(() => ConfirmEventActionAsync("unarchive-event", "events.confirmUnarchive", "events.unarchive"), "unarchive-event");
        AddDayCommand = EventCommand(AddDayAsync, "add-day");
        EditDayCommand = EventCommand(EditDayAsync, "update-day", () => SelectedDay?.DayId is not null);
        RemoveDayCommand = EventCommand(RemoveDayAsync, "remove-day", () => SelectedDay?.DayId is not null);
        RegisterCommand = EventCommand(RegisterAsync, "register-participant", () => _eventProperties?.Days.Count > 0);
        UnregisterCommand = EventCommand(UnregisterAsync, "unregister-participant", () => SelectedParticipant is not null);
        ChangeExperienceCommand = EventCommand(ChangeExperienceAsync, "change-experience", () => SelectedParticipant is not null);
        PreviewCardsCommand = EventCommand(PreviewCardsAsync, "print-event-cards", () => Participants.Count > 0);
        ExportCardsCommand = EventCommand(ExportCardsAsync, "print-event-cards", () => Participants.Count > 0);
    }

    public AsyncCommand PreviousEventsCommand { get; }

    public AsyncCommand NextEventsCommand { get; }

    public AsyncCommand NewEventCommand { get; }

    public AsyncCommand EditEventCommand { get; }

    public AsyncCommand CloseEventCommand { get; }

    public AsyncCommand ReopenEventCommand { get; }

    public AsyncCommand ArchiveEventCommand { get; }

    public AsyncCommand UnarchiveEventCommand { get; }

    public AsyncCommand AddDayCommand { get; }

    public AsyncCommand EditDayCommand { get; }

    public AsyncCommand RemoveDayCommand { get; }

    public AsyncCommand RegisterCommand { get; }

    public AsyncCommand UnregisterCommand { get; }

    public AsyncCommand ChangeExperienceCommand { get; }

    public AsyncCommand PreviewCardsCommand { get; }

    public AsyncCommand ExportCardsCommand { get; }

    public ObservableCollection<LarpEventListItem> Events { get; } = [];

    public ObservableCollection<DayListItem> Days { get; } = [];

    public ObservableCollection<ParticipantListItem> Participants { get; } = [];

    public string EventPage { get; private set; } = string.Empty;

    public LarpEventListItem? SelectedEvent
    {
        get => _selectedEvent;
        set
        {
            if (_selectedEvent == value)
            {
                return;
            }

            _selectedEvent = value;
            OnPropertyChanged();
            _reloadEventCommand.Execute(null);
        }
    }

    public DayListItem? SelectedDay
    {
        get => _selectedDay;
        set
        {
            if (_selectedDay == value)
            {
                return;
            }

            _selectedDay = value;
            OnPropertyChanged();
            NotifyCommandsChanged();
            _reloadParticipantsCommand.Execute(null);
        }
    }

    public ParticipantListItem? SelectedParticipant
    {
        get => _selectedParticipant;
        set
        {
            if (_selectedParticipant == value)
            {
                return;
            }

            _selectedParticipant = value;
            OnPropertyChanged();
            OnPropertyChanged(nameof(ExperienceHistory));
            NotifyCommandsChanged();
        }
    }

    public bool ShowArchived
    {
        get => _showArchived;
        set
        {
            if (_showArchived == value)
            {
                return;
            }

            _showArchived = value;
            OnPropertyChanged();
            _reloadCommand.Execute(null);
        }
    }

    /// <summary>Whether the selected event is Closed, so the screen explains why it cannot change.</summary>
    public bool IsSelectedEventClosed => _eventProperties is { } selected && selected.Status != OpenStatus;

    public string ClosedExplanation => Translate(CanWrite ? "events.closedExplanation" : "events.closedExplanationReader");

    /// <summary>The experience changes given to the selected participant at this event, newest first.</summary>
    public IReadOnlyList<string> ExperienceHistory { get; private set; } = [];

    public string EventsHeading => Translate("api.events");

    public string DaysHeading => Translate("events.days");

    public string ParticipantsHeading => Translate("api.participants");

    public string ShowArchivedLabel => Translate("events.showArchived");

    public string PreviousLabel => Translate("players.previousPage");

    public string NextLabel => Translate("players.nextPage");

    public string NewLabel => Translate("events.new");

    public string EditLabel => Translate("skills.edit");

    public string CloseLabel => Translate("events.close");

    public string ReopenLabel => Translate("events.reopen");

    public string ArchiveLabel => Translate("events.archive");

    public string UnarchiveLabel => Translate("events.unarchive");

    public string AddDayLabel => Translate("api.addDay");

    public string EditDayLabel => Translate("api.updateDay");

    public string RemoveDayLabel => Translate("api.removeDay");

    public string RegisterLabel => Translate("api.registerParticipant");

    public string UnregisterLabel => Translate("api.unregisterParticipant");

    public string ChangeExperienceLabel => Translate("api.changeExperience");

    public string PreviewCardsLabel => Translate("players.previewCards");

    public string ExportCardsLabel => Translate("players.exportCards");

    public string ExperienceHistoryHeading => Translate("events.experienceHistory");

    public string? EventsPlaceholder => Events.Count == 0 ? Translate("events.noEvents") : null;

    public bool HasEventsPlaceholder => EventsPlaceholder is not null;

    public string? DaysPlaceholder => _eventProperties is { Days.Count: 0 } ? Translate("events.noDays") : null;

    public bool HasDaysPlaceholder => DaysPlaceholder is not null;

    public string? ParticipantsPlaceholder => _eventProperties is not null && Participants.Count == 0
        ? Translate("events.noParticipants")
        : null;

    public bool HasParticipantsPlaceholder => ParticipantsPlaceholder is not null;

    public override Task LoadAsync() => RunAsync(() => LoadEventsCoreAsync(null));

    private protected override void OnLocaleChanged()
    {
        foreach (var name in new[]
                 {
                     nameof(EventsHeading), nameof(DaysHeading), nameof(ParticipantsHeading), nameof(ShowArchivedLabel),
                     nameof(PreviousLabel), nameof(NextLabel), nameof(NewLabel), nameof(EditLabel), nameof(CloseLabel),
                     nameof(ReopenLabel), nameof(ArchiveLabel), nameof(UnarchiveLabel), nameof(AddDayLabel),
                     nameof(EditDayLabel), nameof(RemoveDayLabel), nameof(RegisterLabel), nameof(UnregisterLabel),
                     nameof(ChangeExperienceLabel), nameof(PreviewCardsLabel), nameof(ExportCardsLabel),
                     nameof(ExperienceHistoryHeading), nameof(ClosedExplanation)
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
                     PreviousEventsCommand, NextEventsCommand, NewEventCommand, EditEventCommand, CloseEventCommand,
                     ReopenEventCommand, ArchiveEventCommand, UnarchiveEventCommand, AddDayCommand, EditDayCommand,
                     RemoveDayCommand, RegisterCommand, UnregisterCommand, ChangeExperienceCommand, PreviewCardsCommand,
                     ExportCardsCommand
                 })
        {
            command.NotifyCanExecuteChanged();
        }
    }

    private AsyncCommand EventCommand(Func<Task> operation, string actionName, Func<bool>? applies = null) =>
        new(() => RunAsync(operation), HandleUnexpectedFailure,
            () => !IsBusy && _event?.FindAction(actionName) is not null && (applies?.Invoke() ?? true));

    private async Task LoadEventsCoreAsync(string? href)
    {
        var generation = ++_eventLoadGeneration;
        var collection = await GetAsync(href ?? _eventsHref);
        if (href is null && ShowArchived)
        {
            collection = await GetAsync(collection.FindLinkHref(SirenRelations.WithArchived) ?? throw MissingAction());
        }

        if (generation != _eventLoadGeneration)
        {
            return;
        }

        _events = collection;
        IsLoaded = true;
        CanWrite = collection.FindAction("create-event") is not null;
        var page = SirenProperties.Read<LarpEventCollectionProperties>(collection.Properties);
        EventPage = string.Format(CultureInfo.CurrentCulture, Translate("players.page"), page.PageNumber, page.PageCount, page.TotalCount);
        OnPropertyChanged(nameof(EventPage));
        var selectedId = SelectedEvent?.EventId;
        ReplaceAll(Events, (collection.Entities ?? []).Select(entity => ToEventItem(entity, page)));
        _selectedEvent = Events.FirstOrDefault(item => item.EventId == selectedId);
        OnPropertyChanged(nameof(SelectedEvent));
        OnPropertyChanged(nameof(EventsPlaceholder));
        OnPropertyChanged(nameof(HasEventsPlaceholder));
        NotifyCommandsChanged();
        await LoadSelectedEventAsync();
    }

    /// <summary>Loads the selected event's days, keeping the selected day when it still exists, then its participants.</summary>
    private async Task LoadSelectedEventAsync()
    {
        var generation = ++_participantLoadGeneration;
        var resource = SelectedEvent is { } selected ? await GetAsync(selected.Href) : null;
        if (generation != _participantLoadGeneration)
        {
            return;
        }

        _event = resource;
        _eventProperties = resource is null ? null : SirenProperties.Read<LarpEventProperties>(resource.Properties);
        var selectedDayId = SelectedDay?.DayId;
        ReplaceAll(Days, _eventProperties is null ? [] : DayItems(_eventProperties));
        _selectedDay = Days.FirstOrDefault(day => day.DayId == selectedDayId) ?? Days.FirstOrDefault();
        OnPropertyChanged(nameof(SelectedDay));
        OnPropertyChanged(nameof(IsSelectedEventClosed));
        OnPropertyChanged(nameof(ClosedExplanation));
        OnPropertyChanged(nameof(DaysPlaceholder));
        OnPropertyChanged(nameof(HasDaysPlaceholder));
        NotifyCommandsChanged();
        await LoadParticipantsAsync();
    }

    private async Task LoadParticipantsAsync()
    {
        var generation = ++_participantLoadGeneration;
        SirenEntity? collection = null;
        if (_event?.FindAction("list-participants") is { } list && SelectedDay is not null)
        {
            collection = await GetAsync(list.ToQueryHref(new Dictionary<string, string?>
            {
                ["dayId"] = SelectedDay.DayId?.ToString("D")
            }));
        }

        if (generation != _participantLoadGeneration)
        {
            return;
        }

        var selectedId = SelectedParticipant?.CharacterId;
        var participants = (collection?.Entities ?? []).Select(entity => SirenProperties.Read<ParticipantProperties>(entity.Properties)).ToArray();
        _participantDetails = participants.ToDictionary(participant => participant.CharacterId);
        ReplaceAll(Participants, participants.Select(ToParticipantItem));
        _selectedParticipant = Participants.FirstOrDefault(participant => participant.CharacterId == selectedId);
        OnPropertyChanged(nameof(SelectedParticipant));
        UpdateExperienceHistory();
        OnPropertyChanged(nameof(ParticipantsPlaceholder));
        OnPropertyChanged(nameof(HasParticipantsPlaceholder));
        NotifyCommandsChanged();
    }

    private void UpdateExperienceHistory()
    {
        ExperienceHistory = SelectedParticipant is { } selected && _participantDetails.TryGetValue(selected.CharacterId, out var details)
            ? details.ExperienceChanges
                .Reverse()
                .Select(ExperienceEntry)
                .ToArray()
            : [];
        OnPropertyChanged(nameof(ExperienceHistory));
    }

    private string ExperienceEntry(ExperienceChangeProperties change)
    {
        var amount = change.Amount.ToString("+#;-#;0", CultureInfo.CurrentCulture);
        var when = change.ChangedAtUtc.ToLocalTime().ToString("g", CultureInfo.CurrentCulture);
        return change.IsClosingAward
            ? string.Format(CultureInfo.CurrentCulture, Translate("events.closingAwardEntry"), amount, when)
            : string.Format(CultureInfo.CurrentCulture, Translate("events.experienceEntry"), amount, change.ChangedBy ?? "?", when);
    }

    private async Task CreateEventAsync()
    {
        var action = _events?.FindAction("create-event") ?? throw MissingAction();
        SirenEntity? saved = null;
        var editor = new LarpEventEditorViewModel(Localizer, Locale, "events.newEvent",
            editor => SubmitEditorAsync(editor, async () => saved = await SubmitAsync(action, editor.CreateBody(null)), null));
        await _dialogs.ShowEventEditorAsync(editor);
        if (saved is not null)
        {
            await LoadEventsCoreAsync(null);
            SelectEvent(SirenProperties.Read<LarpEventProperties>(saved.Properties).EventId);
            StatusMessage = Translate("actions.completed");
        }
    }

    private async Task EditEventAsync()
    {
        var current = await GetAsync(SelectedEvent?.Href ?? throw MissingAction());
        var properties = SirenProperties.Read<LarpEventProperties>(current.Properties);
        var editor = new LarpEventEditorViewModel(Localizer, Locale, "events.editEvent",
            editor => SubmitEditorAsync(
                editor,
                async () => current = await SubmitAsync(
                    current.FindAction("update-event") ?? throw MissingAction(),
                    editor.CreateBody(SirenProperties.Read<LarpEventProperties>(current.Properties).Version)),
                async () =>
                {
                    current = await GetAsync(current.FindLinkHref("self") ?? throw MissingAction());
                    var latest = SirenProperties.Read<LarpEventProperties>(current.Properties);
                    editor.Name = latest.Name;
                    editor.Description = latest.Description ?? string.Empty;
                    editor.BaseExperience = ExperienceValueText.Format(latest.BaseExperience);
                }))
        {
            Name = properties.Name,
            Description = properties.Description ?? string.Empty,
            BaseExperience = ExperienceValueText.Format(properties.BaseExperience)
        };
        await _dialogs.ShowEventEditorAsync(editor);
        await ReloadAfterDialogAsync(editor.IsSaved);
    }

    /// <summary>
    /// Shows the closing grid built from the latest event and its participants. A close from a grid that is no
    /// longer current is refused as stale, and the grid reloads with the latest data.
    /// </summary>
    private async Task CloseEventAsync()
    {
        var (resource, larpEvent, participants) = await LoadClosingDataAsync();
        var action = resource.FindAction("close-event") ?? throw MissingAction();
        var editor = new CloseEventViewModel(
            Localizer,
            Locale,
            larpEvent,
            participants,
            () => ConfirmAsync("events.confirmCloseAwards", "events.close", larpEvent.Name),
            editor => SubmitEditorAsync(
                editor,
                () => SubmitAsync(action, editor.CreateBody()),
                async () =>
                {
                    var (latest, latestEvent, latestParticipants) = await LoadClosingDataAsync();
                    action = latest.FindAction("close-event") ?? throw MissingAction();
                    editor.Load(latestEvent, latestParticipants);
                }));
        await _dialogs.ShowCloseEventAsync(editor);
        await ReloadAfterDialogAsync(editor.IsSaved);
    }

    private async Task<(SirenEntity Resource, LarpEventProperties Event, IReadOnlyList<ParticipantProperties> Participants)> LoadClosingDataAsync()
    {
        var resource = await GetAsync(SelectedEvent?.Href ?? throw MissingAction());
        var list = resource.FindAction("list-participants") ?? throw MissingAction();
        var participants = await GetAsync(list.ToQueryHref(new Dictionary<string, string?>()));
        return (
            resource,
            SirenProperties.Read<LarpEventProperties>(resource.Properties),
            (participants.Entities ?? []).Select(entity => SirenProperties.Read<ParticipantProperties>(entity.Properties)).ToArray());
    }

    private async Task ConfirmEventActionAsync(string actionName, string messageKey, string confirmKey)
    {
        var selected = SelectedEvent ?? throw MissingAction();
        var resource = await GetAsync(selected.Href);
        var action = resource.FindAction(actionName) ?? throw MissingAction();
        if (!await ConfirmAsync(messageKey, confirmKey, selected.Name))
        {
            return;
        }

        await SubmitAsync(action, null);
        await LoadEventsCoreAsync(_events?.FindLinkHref("self"));
        StatusMessage = Translate("actions.completed");
    }

    private async Task AddDayAsync()
    {
        var action = _event?.FindAction("add-day") ?? throw MissingAction();
        var editor = new DayEditorViewModel(Localizer, Locale, "events.addDay", NextSuggestedDate(), null,
            editor => SubmitEditorAsync(editor, () => SubmitAsync(action, editor.CreateBody(null)), null));
        await _dialogs.ShowDayEditorAsync(editor);
        await ReloadAfterDialogAsync(editor.IsSaved);
    }

    private async Task EditDayAsync()
    {
        var action = _event?.FindAction("update-day") ?? throw MissingAction();
        var day = SelectedDay ?? throw MissingAction();
        var editor = new DayEditorViewModel(Localizer, Locale, "events.editDay", day.Date, day.Experience,
            editor => SubmitEditorAsync(editor, () => SubmitAsync(action, editor.CreateBody(day.DayId)), null));
        await _dialogs.ShowDayEditorAsync(editor);
        await ReloadAfterDialogAsync(editor.IsSaved);
    }

    private async Task RemoveDayAsync()
    {
        var action = _event?.FindAction("remove-day") ?? throw MissingAction();
        var day = SelectedDay ?? throw MissingAction();
        if (!await ConfirmAsync("events.confirmRemoveDay", "api.removeDay", day.DisplayName))
        {
            return;
        }

        await SubmitAsync(action, JsonSerializer.SerializeToElement(new { dayId = day.DayId }, JsonOptions));
        await ReloadAfterDialogAsync(saved: true);
    }

    private async Task RegisterAsync()
    {
        var action = _event?.FindAction("register-participant") ?? throw MissingAction();
        var properties = _eventProperties ?? throw MissingAction();
        var allParticipants = await GetAsync((_event.FindAction("list-participants") ?? throw MissingAction())
            .ToQueryHref(new Dictionary<string, string?>()));
        var registered = (allParticipants.Entities ?? [])
            .Select(entity => SirenProperties.Read<ParticipantProperties>(entity.Properties))
            .ToDictionary(participant => participant.CharacterId, participant => participant.DayIds);
        var days = properties.Days.Select(day => new DayChoice(day.DayId, day.Date.ToString(IsoDate, CultureInfo.InvariantCulture))
        {
            IsSelected = SelectedDay?.DayId is not { } selected || selected == day.DayId
        }).ToArray();
        var editor = new RegistrationEditorViewModel(Localizer, Locale, days, registered, SearchCharactersAsync,
            editor => SubmitEditorAsync(editor, () => SubmitAsync(action, editor.CreateBody()), null));
        await _dialogs.ShowRegistrationAsync(editor);
        await ReloadAfterDialogAsync(editor.IsSaved);
    }

    private async Task<IReadOnlyList<CharacterChoice>> SearchCharactersAsync(string text)
    {
        var characters = await GetAsync(_event?.FindLinkHref(SirenRelations.Characters) ?? throw MissingAction());
        var search = characters.FindAction("search-characters") ?? throw MissingAction();
        var results = await GetAsync(search.ToQueryHref(new Dictionary<string, string?> { ["search"] = text }));
        return (results.Entities ?? [])
            .Select(entity => SirenProperties.Read<CharacterProperties>(entity.Properties))
            .Select(character => new CharacterChoice(
                character.CharacterId,
                $"#{character.Number} {character.Name} — {character.PlayerDisplayName}"))
            .ToArray();
    }

    /// <summary>
    /// Unregisters the selected participant from the selected day, or from every registered day when the list shows
    /// every day.
    /// </summary>
    private async Task UnregisterAsync()
    {
        var action = _event?.FindAction("unregister-participant") ?? throw MissingAction();
        var participant = SelectedParticipant ?? throw MissingAction();
        Guid[] dayIds = SelectedDay?.DayId is { } dayId ? [dayId] : [.. participant.DayIds];
        var messageKey = SelectedDay?.DayId is null ? "events.confirmUnregisterAll" : "events.confirmUnregisterDay";
        if (!await ConfirmAsync(messageKey, "api.unregisterParticipant", participant.CharacterName, SelectedDay?.DisplayName ?? string.Empty))
        {
            return;
        }

        await SubmitAsync(action, JsonSerializer.SerializeToElement(new { characterId = participant.CharacterId, dayIds }, JsonOptions));
        await ReloadAfterDialogAsync(saved: true);
    }

    private async Task ChangeExperienceAsync()
    {
        var action = _event?.FindAction("change-experience") ?? throw MissingAction();
        var editor = new ExperienceEditorViewModel(Localizer, Locale, SelectedParticipant ?? throw MissingAction(),
            editor => SubmitEditorAsync(
                editor,
                () => editor.TryReadAmount(out var amount)
                    ? SubmitAsync(action, editor.CreateBody(amount))
                    : throw new InvalidOperationException("The amount was checked before submitting."),
                null));
        await _dialogs.ShowExperienceAsync(editor);
        await ReloadAfterDialogAsync(editor.IsSaved);
    }

    private async Task PreviewCardsAsync()
    {
        if (!await _dialogs.PreviewPdfAsync(await PrintCardsAsync(), CardsFileName))
        {
            StatusMessage = Translate("players.cardsPreviewFailed");
        }
    }

    private async Task ExportCardsAsync()
    {
        if (await _dialogs.ExportPdfAsync(await PrintCardsAsync(), CardsFileName, Translate("players.cardsFileType")))
        {
            StatusMessage = Translate("players.cardsExported");
        }
    }

    /// <summary>Prints the cards of the characters registered for the selected day, or for every day.</summary>
    private Task<byte[]> PrintCardsAsync() =>
        ApiClient.DownloadActionAsync(
            ApiBaseUrl,
            _event?.FindAction("print-event-cards") ?? throw MissingAction(),
            JsonSerializer.SerializeToElement(new { dayId = SelectedDay?.DayId }, JsonOptions),
            PdfMediaType,
            Locale);

    private async Task ReloadAfterDialogAsync(bool saved)
    {
        if (!saved)
        {
            return;
        }

        await LoadEventsCoreAsync(_events?.FindLinkHref("self"));
        StatusMessage = Translate("actions.completed");
    }

    private Task<bool> ConfirmAsync(string messageKey, string confirmKey, params object[] arguments) =>
        _dialogs.ConfirmAsync(new ConfirmationRequest(
            Translate("skills.confirmTitle"),
            string.Format(CultureInfo.CurrentCulture, Translate(messageKey), arguments),
            Translate(confirmKey),
            Translate("common.cancel")));

    private void SelectEvent(Guid eventId)
    {
        if (Events.FirstOrDefault(item => item.EventId == eventId) is { } created)
        {
            SelectedEvent = created;
        }
    }

    /// <summary>The day after the event's last day, or today for an event without days.</summary>
    private DateOnly NextSuggestedDate() =>
        _eventProperties?.LastDate?.AddDays(1) ?? DateOnly.FromDateTime(DateTime.Today);

    private IEnumerable<DayListItem> DayItems(LarpEventProperties larpEvent) =>
    [
        new DayListItem(null, null, null, string.Format(CultureInfo.CurrentCulture, Translate("events.allDaysCount"), larpEvent.ParticipantCount)),
        .. larpEvent.Days.Select(day => new DayListItem(day.DayId, day.Date, day.Experience, DayEntry(day)))
    ];

    private string DayEntry(LarpEventDayProperties day)
    {
        var date = day.Date.ToString(IsoDate, CultureInfo.InvariantCulture);
        return day.Experience is { } experience
            ? string.Format(CultureInfo.CurrentCulture, Translate("events.dayEntryExperience"), date, day.ParticipantCount, experience)
            : string.Format(CultureInfo.CurrentCulture, Translate("events.dayEntry"), date, day.ParticipantCount);
    }

    private LarpEventListItem ToEventItem(SirenSubEntity entity, LarpEventCollectionProperties page)
    {
        var larpEvent = SirenProperties.Read<LarpEventProperties>(entity.Properties);
        var statusTitle = page.Statuses.FirstOrDefault(status => status.Name == larpEvent.Status)?.Title ?? larpEvent.Status;
        var dates = larpEvent.FirstDate is { } first
            ? first == larpEvent.LastDate
                ? first.ToString(IsoDate, CultureInfo.InvariantCulture)
                : $"{first.ToString(IsoDate, CultureInfo.InvariantCulture)} – {larpEvent.LastDate?.ToString(IsoDate, CultureInfo.InvariantCulture)}"
            : Translate("events.noDates");
        var tags = new List<string> { $"({statusTitle})" };
        if (larpEvent.IsArchived)
        {
            tags.Add(Translate("events.archivedTag"));
        }

        return new LarpEventListItem(
            larpEvent.EventId,
            larpEvent.Name,
            larpEvent.Status == OpenStatus,
            larpEvent.IsArchived,
            entity.Href ?? throw MissingAction(),
            string.Join(' ', [$"#{larpEvent.Number} {larpEvent.Name} · {dates}", .. tags]));
    }

    private ParticipantListItem ToParticipantItem(ParticipantProperties participant) =>
        new(
            participant.CharacterId,
            participant.CharacterName,
            participant.DayIds,
            participant.ExperiencePoints,
            participant.ExperienceTotal,
            string.Format(
                CultureInfo.CurrentCulture,
                Translate("events.participantEntry"),
                participant.CharacterNumber,
                participant.CharacterName,
                participant.PlayerDisplayName,
                string.Join(", ", participant.Dates.Select(date => date.ToString(IsoDate, CultureInfo.InvariantCulture))),
                participant.ExperienceTotal.ToString("+#;-#;0", CultureInfo.CurrentCulture)));
}
