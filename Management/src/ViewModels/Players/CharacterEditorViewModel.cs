using System.Collections.ObjectModel;
using System.Globalization;
using System.Text.Json;
using System.Text.Json.Nodes;
using SciFiLarpNetwork.Management.Api.Players;
using SciFiLarpNetwork.Management.Localization;

namespace SciFiLarpNetwork.Management.ViewModels.Players;

/// <summary>
/// The character dialog. Status and death date can be changed only for an existing character; a new character is
/// always Active. The default network is shown as included and cannot be removed, because it is never stored on a
/// character.
/// </summary>
public sealed class CharacterEditorViewModel : CatalogEditorViewModel
{
    private const string NumberType = "number";
    private const string DeadStatus = "dead";
    private readonly Func<CharacterEditorViewModel, Task<bool>> _submit;
    private CharacterStatRowViewModel[] _statRows = [];
    private string _characterTitle = string.Empty;
    private string _species = string.Empty;
    private string _gender = string.Empty;
    private string _characterType = string.Empty;
    private string _experiencePoints = "0";
    private RosterChoice? _selectedStatus;
    private DateTimeOffset? _deathDate;
    private NetworkChoice? _selectedNetwork;
    private string _defaultNetwork = string.Empty;
    private string _qrIdentifier = string.Empty;
    private bool _showArchivedStats;
    private Func<Task>? _manageSkills;

    internal CharacterEditorViewModel(
        IManagementLocalizer localizer,
        string locale,
        string titleKey,
        IReadOnlyList<RosterChoice> statuses,
        IReadOnlyList<RosterChoice> statTypes,
        bool isExisting,
        Func<CharacterEditorViewModel, Task<bool>> submit)
        : base(localizer, locale, titleKey)
    {
        Statuses = statuses;
        StatTypes = statTypes;
        IsExisting = isExisting;
        _selectedStatus = statuses.Count > 0 ? statuses[0] : null;
        _submit = submit;
        AddNewStatCommand = new DelegateCommand(() => NewStats.Add(new NewCharacterStatRowViewModel(StatTypes, row => NewStats.Remove(row))));
        ManageSkillsCommand = new AsyncCommand(
            () => _manageSkills?.Invoke() ?? Task.CompletedTask,
            HandleManageSkillsFailure,
            () => _manageSkills is not null && !IsSaving);
        PropertyChanged += (_, e) =>
        {
            if (e.PropertyName == nameof(IsSaving))
            {
                ManageSkillsCommand.NotifyCanExecuteChanged();
            }
        };
    }

    /// <summary>Opens the character's talents and skills window; available only for a saved character.</summary>
    public AsyncCommand ManageSkillsCommand { get; }

    public bool CanManageSkills => _manageSkills is not null;

    public string ManageSkillsLabel => Translate("characterSkills.manage");

    public IReadOnlyList<RosterChoice> Statuses { get; }

    public IReadOnlyList<RosterChoice> StatTypes { get; }

    /// <summary>Whether the dialog edits a saved character; only then can the status change.</summary>
    public bool IsExisting { get; }

    public DelegateCommand AddNewStatCommand { get; }

    public ObservableCollection<NetworkChoice> Networks { get; } = [];

    /// <summary>The stats shown: every stat when <see cref="ShowArchivedStats"/> is set, otherwise the unarchived ones.</summary>
    public ObservableCollection<CharacterStatRowViewModel> Stats { get; } = [];

    public ObservableCollection<NewCharacterStatRowViewModel> NewStats { get; } = [];

    public string CharacterNameLabel => Translate("characters.name");

    public string CharacterTitleLabel => Translate("characters.title");

    public string SpeciesLabel => Translate("characters.species");

    public string GenderLabel => Translate("characters.gender");

    public string CharacterTypeLabel => Translate("characters.type");

    public string ExperiencePointsLabel => Translate("characters.experiencePoints");

    public string StatusLabel => Translate("characters.status");

    public string DeathDateLabel => Translate("characters.deathDate");

    public string NetworksLabel => Translate("characters.networks");

    public string NetworkLabel => Translate("characters.network");

    public string QrIdentifierLabel => Translate("characters.qrIdentifier");

    public string StatsLabel => Translate("characters.stats");

    public string ShowArchivedStatsLabel => Translate("stats.showArchived");

    public string NewStatsLabel => Translate("characters.newStats");

    public string NewStatsHelp => Translate("characters.newStatsHelp");

    public string AddNewStatLabel => Translate("characters.addStat");

    public string RemoveLabel => Translate("skills.remove");

    public string StatNameLabel => Translate("stats.name");

    public string StatTypeLabel => Translate("stats.type");

    public string StatValueLabel => Translate("stats.value");

    public string CharacterTitle
    {
        get => _characterTitle;
        set => SetField(ref _characterTitle, value);
    }

    public string Species
    {
        get => _species;
        set => SetField(ref _species, value);
    }

    public string Gender
    {
        get => _gender;
        set => SetField(ref _gender, value);
    }

    public string CharacterType
    {
        get => _characterType;
        set => SetField(ref _characterType, value);
    }

    public string ExperiencePoints
    {
        get => _experiencePoints;
        set => SetField(ref _experiencePoints, value);
    }

    public RosterChoice? SelectedStatus
    {
        get => _selectedStatus;
        set
        {
            if (_selectedStatus == value)
            {
                return;
            }

            _selectedStatus = value;
            OnPropertyChanged();
            OnPropertyChanged(nameof(IsDead));
        }
    }

    public bool IsDead => SelectedStatus?.Name == DeadStatus;

    public DateTimeOffset? DeathDate
    {
        get => _deathDate;
        set
        {
            if (_deathDate == value)
            {
                return;
            }

            _deathDate = value;
            OnPropertyChanged();
        }
    }

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
        }
    }

    /// <summary>The default network every character may use, or a note that no default network is set.</summary>
    public string DefaultNetwork
    {
        get => _defaultNetwork;
        private set => SetField(ref _defaultNetwork, value);
    }

    public string QrIdentifier
    {
        get => _qrIdentifier;
        private set => SetField(ref _qrIdentifier, value);
    }

    public bool ShowArchivedStats
    {
        get => _showArchivedStats;
        set
        {
            if (_showArchivedStats == value)
            {
                return;
            }

            _showArchivedStats = value;
            OnPropertyChanged();
            ShowStats();
        }
    }

    internal void EnableManageSkills(Func<Task> manageSkills)
    {
        _manageSkills = manageSkills;
        OnPropertyChanged(nameof(CanManageSkills));
        ManageSkillsCommand.NotifyCanExecuteChanged();
    }

    private void HandleManageSkillsFailure(Exception exception)
    {
        System.Diagnostics.Trace.TraceError("Unhandled talents and skills exception: {0}", exception);
        ErrorMessage = Translate("common.error");
    }

    internal void SetNetworks(string defaultNetwork, IReadOnlyList<NetworkChoice> choices, Guid? selectedNetworkId)
    {
        DefaultNetwork = defaultNetwork;
        Networks.Clear();
        foreach (var choice in choices)
        {
            Networks.Add(choice);
        }

        SelectedNetwork = Networks.FirstOrDefault(choice => choice.NetworkId == selectedNetworkId) ?? Networks.FirstOrDefault();
    }

    internal void SetStats(IEnumerable<CharacterStatValueProperties> stats)
    {
        _statRows = stats
            .Select(stat => new CharacterStatRowViewModel(
                stat.StatId,
                stat.Name,
                stat.Type == NumberType,
                stat.IsArchived,
                stat.Type == NumberType
                    ? (stat.Number ?? 0).ToString(CultureInfo.CurrentCulture)
                    : stat.Text ?? string.Empty))
            .ToArray();
        ShowStats();
    }

    internal void Apply(CharacterProperties character)
    {
        Name = character.Name;
        CharacterTitle = character.Title ?? string.Empty;
        Species = character.Species ?? string.Empty;
        Gender = character.Gender ?? string.Empty;
        CharacterType = character.Type ?? string.Empty;
        ExperiencePoints = character.ExperiencePoints.ToString(CultureInfo.CurrentCulture);
        SelectedStatus = Statuses.FirstOrDefault(status => status.Name == character.Status) ?? SelectedStatus;
        DeathDate = character.DeathDate is { } date
            ? new DateTimeOffset(date.ToDateTime(TimeOnly.MinValue), TimeSpan.Zero)
            : null;
        QrIdentifier = character.QrIdentifier;
        NewStats.Clear();
        SetStats(character.Stats);
    }

    /// <summary>
    /// Checks that experience points and Number stat values are whole numbers of 0 or more; otherwise shows a
    /// localized error and returns <see langword="false"/>.
    /// </summary>
    internal bool ValidateNumbers()
    {
        if (ParseNumber(ExperiencePoints) is null)
        {
            ErrorMessage = Localizer.TranslateApiError("errors.invalidExperiencePoints", Locale);
            return false;
        }

        var statsValid = _statRows.All(row => StatValue(row.IsNumber, row.Value) is not null) &&
                         NewStats.All(row => StatValue(row.SelectedType?.Name == NumberType, row.Value) is not null);
        if (!statsValid)
        {
            ErrorMessage = Localizer.TranslateApiError("errors.invalidStatValue", Locale);
        }

        return statsValid;
    }

    /// <summary>Builds the request body; call <see cref="ValidateNumbers"/> first.</summary>
    internal JsonElement CreateBody(long? version)
    {
        var statValues = new JsonArray();
        foreach (var row in _statRows)
        {
            var value = StatValue(row.IsNumber, row.Value)!;
            value["statId"] = row.StatId;
            statValues.Add(value);
        }

        var newStats = new JsonArray();
        foreach (var row in NewStats)
        {
            var value = StatValue(row.SelectedType?.Name == NumberType, row.Value)!;
            value["name"] = row.Name;
            value["type"] = row.SelectedType?.Name;
            newStats.Add(value);
        }

        var body = new JsonObject
        {
            ["name"] = Name,
            ["title"] = Blank(CharacterTitle),
            ["species"] = Blank(Species),
            ["gender"] = Blank(Gender),
            ["type"] = Blank(CharacterType),
            ["experiencePoints"] = ParseNumber(ExperiencePoints),
            ["networkId"] = SelectedNetwork?.NetworkId,
            ["statValues"] = statValues,
            ["newStats"] = newStats
        };
        if (version is { } savedVersion)
        {
            body["version"] = savedVersion;
            body["status"] = SelectedStatus?.Name;
            body["deathDate"] = IsDead && DeathDate is { } date ? date.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture) : null;
        }

        return JsonSerializer.SerializeToElement(body);
    }

    private protected override Task<bool> SubmitAsync() => _submit(this);

    private void ShowStats()
    {
        Stats.Clear();
        foreach (var row in _statRows.Where(row => ShowArchivedStats || !row.IsArchived))
        {
            Stats.Add(row);
        }
    }

    private static JsonObject? StatValue(bool isNumber, string value)
    {
        if (!isNumber)
        {
            return new JsonObject { ["text"] = Blank(value) };
        }

        return (string.IsNullOrWhiteSpace(value) ? 0 : ParseNumber(value)) is { } number
            ? new JsonObject { ["number"] = number }
            : null;
    }

    private static int? ParseNumber(string value) =>
        int.TryParse(value.Trim(), NumberStyles.None, CultureInfo.CurrentCulture, out var number) ? number : null;

    private static string? Blank(string value) => string.IsNullOrWhiteSpace(value) ? null : value;

    private void SetField(ref string field, string value, [System.Runtime.CompilerServices.CallerMemberName] string? propertyName = null)
    {
        if (field == value)
        {
            return;
        }

        field = value;
        OnPropertyChanged(propertyName);
    }
}
