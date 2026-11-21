using System.Collections.ObjectModel;
using System.Diagnostics;
using System.Globalization;
using System.Text.Json;
using SciFiLarpNetwork.Management.Localization;

namespace SciFiLarpNetwork.Management.ViewModels.LarpEvents;

/// <summary>
/// The registration dialog: find a character, then pick days. Days the character is already registered for are shown
/// as such and cannot be picked, so only new days are sent.
/// </summary>
public sealed class RegistrationEditorViewModel : CatalogEditorViewModel
{
    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web);
    private readonly Func<string, Task<IReadOnlyList<CharacterChoice>>> _search;
    private readonly IReadOnlyDictionary<Guid, IReadOnlyList<Guid>> _registeredDays;
    private readonly Func<RegistrationEditorViewModel, Task<bool>> _submit;
    private string _searchText = string.Empty;
    private CharacterChoice? _selectedCharacter;
    private string? _searchMessage;

    /// <param name="registeredDays">The days each registered character already has, by character identifier.</param>
    internal RegistrationEditorViewModel(
        IManagementLocalizer localizer,
        string locale,
        IReadOnlyList<DayChoice> days,
        IReadOnlyDictionary<Guid, IReadOnlyList<Guid>> registeredDays,
        Func<string, Task<IReadOnlyList<CharacterChoice>>> search,
        Func<RegistrationEditorViewModel, Task<bool>> submit)
        : base(localizer, locale, "events.registerCharacter")
    {
        Days = days;
        _registeredDays = registeredDays;
        _search = search;
        _submit = submit;
        SearchCommand = new AsyncCommand(SearchAsync, HandleSearchFailure, () => !string.IsNullOrWhiteSpace(SearchText));
        SelectAllDaysCommand = new DelegateCommand(SelectAllDays);
    }

    public AsyncCommand SearchCommand { get; }

    public DelegateCommand SelectAllDaysCommand { get; }

    public IReadOnlyList<DayChoice> Days { get; }

    public ObservableCollection<CharacterChoice> Characters { get; } = [];

    public string CharacterLabel => Translate("events.character");

    public string SearchLabel => Translate("roster.search");

    public string SearchButtonLabel => Translate("players.search");

    public string DaysLabel => Translate("events.days");

    public string AllDaysLabel => Translate("events.allDays");

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

    public CharacterChoice? SelectedCharacter
    {
        get => _selectedCharacter;
        set
        {
            if (_selectedCharacter == value)
            {
                return;
            }

            _selectedCharacter = value;
            OnPropertyChanged();
            MarkRegisteredDays();
        }
    }

    /// <summary>Explains which days the chosen character already has; empty when they have none.</summary>
    public string AlreadyRegisteredText
    {
        get
        {
            var registered = Days.Where(day => day.IsRegistered).Select(day => day.Label).ToArray();
            return registered.Length == 0
                ? string.Empty
                : string.Format(CultureInfo.CurrentCulture, Translate("events.alreadyRegisteredOn"), string.Join(", ", registered));
        }
    }

    public bool HasAlreadyRegisteredText => AlreadyRegisteredText.Length > 0;

    internal JsonElement CreateBody() =>
        JsonSerializer.SerializeToElement(
            new
            {
                characterId = SelectedCharacter?.CharacterId,
                dayIds = Days.Where(day => day.IsSelected && !day.IsRegistered).Select(day => day.DayId).ToArray()
            },
            JsonOptions);

    private protected override async Task<bool> SubmitAsync()
    {
        if (SelectedCharacter is null)
        {
            ErrorMessage = Translate("events.characterRequired");
            return false;
        }

        if (!Days.Any(day => day.IsSelected && !day.IsRegistered))
        {
            ErrorMessage = Translate(Days.All(day => day.IsRegistered) ? "errors.alreadyRegistered" : "errors.noDaysSelected");
            return false;
        }

        return await _submit(this);
    }

    private async Task SearchAsync()
    {
        SearchMessage = null;
        var found = await _search(SearchText);
        Characters.Clear();
        foreach (var character in found)
        {
            Characters.Add(character);
        }

        SelectedCharacter = Characters.FirstOrDefault();
        SearchMessage = Characters.Count == 0 ? Translate("common.noResults") : null;
    }

    private void SelectAllDays()
    {
        foreach (var day in Days.Where(day => !day.IsRegistered))
        {
            day.IsSelected = true;
        }
    }

    private void MarkRegisteredDays()
    {
        var registered = SelectedCharacter is { } character
            ? _registeredDays.GetValueOrDefault(character.CharacterId) ?? []
            : [];
        foreach (var day in Days)
        {
            day.IsRegistered = registered.Contains(day.DayId);
            if (day.IsRegistered)
            {
                day.IsSelected = false;
            }
        }

        OnPropertyChanged(nameof(AlreadyRegisteredText));
        OnPropertyChanged(nameof(HasAlreadyRegisteredText));
    }

    private void HandleSearchFailure(Exception exception)
    {
        Trace.TraceError("Character search failed: {0}", exception);
        SearchMessage = Translate("common.error");
    }
}
