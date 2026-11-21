using System.Collections.ObjectModel;
using System.Globalization;
using System.Text.Json;
using SciFiLarpNetwork.Management.Api.LarpEvents;
using SciFiLarpNetwork.Management.Localization;

namespace SciFiLarpNetwork.Management.ViewModels.LarpEvents;

/// <summary>
/// The close dialog: a grid of the participants and the event's days where the administrator picks the experience
/// each character gets. Experience an earlier close already gave shows as given and cannot be picked again. Cells
/// start ticked, except for characters that are Retired, Dead, or archived. Closing asks for a final confirmation.
/// </summary>
public sealed class CloseEventViewModel : CatalogEditorViewModel
{
    private const string ActiveStatus = "active";
    private const string IsoDate = "yyyy-MM-dd";
    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web);
    private readonly Func<Task<bool>> _confirm;
    private readonly Func<CloseEventViewModel, Task<bool>> _submit;
    private bool _hasBaseColumn;

    internal CloseEventViewModel(
        IManagementLocalizer localizer,
        string locale,
        LarpEventProperties larpEvent,
        IReadOnlyList<ParticipantProperties> participants,
        Func<Task<bool>> confirm,
        Func<CloseEventViewModel, Task<bool>> submit)
        : base(localizer, locale, "events.closeEvent")
    {
        _confirm = confirm;
        _submit = submit;
        ToggleAllDaysCommand = new DelegateCommand(() => Toggle(Rows.SelectMany(row => row.DayCells)));
        ToggleAllBaseCommand = new DelegateCommand(() => Toggle(Rows.Select(row => row.BaseCell).OfType<ClosingCellViewModel>()));
        Load(larpEvent, participants);
    }

    public DelegateCommand ToggleAllDaysCommand { get; }

    public DelegateCommand ToggleAllBaseCommand { get; }

    public ObservableCollection<string> DayHeaders { get; } = [];

    public ObservableCollection<ClosingRowViewModel> Rows { get; } = [];

    public string BaseHeader { get; private set; } = string.Empty;

    public bool HasBaseColumn
    {
        get => _hasBaseColumn;
        private set
        {
            if (_hasBaseColumn == value)
            {
                return;
            }

            _hasBaseColumn = value;
            OnPropertyChanged();
        }
    }

    public long Version { get; private set; }

    public bool HasAnythingToAward => Rows.Any(row => row.BaseCell is { IsCheckbox: true } || row.DayCells.Any(cell => cell.IsCheckbox));

    public bool HasNothingToAward => !HasAnythingToAward;

    public string Warning => Translate("events.closeWarning");

    public string NothingToAwardText => Translate("events.nothingToAward");

    public string CharacterHeader => Translate("events.closeCharacter");

    public string ToggleAllDaysLabel => Translate("events.toggleAllDays");

    public string ToggleAllBaseLabel => Translate("events.toggleAllBase");

    public string CloseLabel => Translate("events.close");

    /// <summary>Rebuilds the grid from the latest event and participants, as after a stale close.</summary>
    internal void Load(LarpEventProperties larpEvent, IReadOnlyList<ParticipantProperties> participants)
    {
        Version = larpEvent.Version;
        HasBaseColumn = larpEvent.BaseExperience is not null;
        BaseHeader = larpEvent.BaseExperience is { } baseExperience
            ? string.Format(CultureInfo.CurrentCulture, Translate("events.closeBase"), baseExperience)
            : string.Empty;
        OnPropertyChanged(nameof(BaseHeader));
        DayHeaders.Clear();
        foreach (var day in larpEvent.Days)
        {
            var date = day.Date.ToString(IsoDate, CultureInfo.InvariantCulture);
            DayHeaders.Add(day.Experience is { } experience
                ? string.Format(CultureInfo.CurrentCulture, Translate("events.closeDay"), date, experience)
                : $"{date} (—)");
        }

        Rows.Clear();
        foreach (var participant in participants)
        {
            Rows.Add(Row(larpEvent, participant));
        }

        OnPropertyChanged(nameof(HasAnythingToAward));
        OnPropertyChanged(nameof(HasNothingToAward));
    }

    internal JsonElement CreateBody() =>
        JsonSerializer.SerializeToElement(
            new
            {
                version = Version,
                awards = Rows
                    .Select(row => new
                    {
                        characterId = row.CharacterId,
                        baseExperience = row.BaseCell is { IsCheckbox: true, IsChecked: true },
                        dayIds = row.DayCells.Where(cell => cell is { IsCheckbox: true, IsChecked: true }).Select(cell => cell.DayId!.Value).ToArray()
                    })
                    .Where(award => award.baseExperience || award.dayIds.Length > 0)
                    .ToArray()
            },
            JsonOptions);

    private protected override async Task<bool> SubmitAsync() => await _confirm() && await _submit(this);

    private ClosingRowViewModel Row(LarpEventProperties larpEvent, ParticipantProperties participant)
    {
        var tickedByDefault = participant.Status == ActiveStatus && !participant.IsArchived;
        var baseCell = larpEvent.BaseExperience is null
            ? null
            : participant.BaseExperienceGiven
                ? ClosingCellViewModel.Label(null, Translate("events.given"))
                : ClosingCellViewModel.Checkbox(null, tickedByDefault);
        var dayCells = larpEvent.Days.Select(day =>
                !participant.DayIds.Contains(day.DayId) ? ClosingCellViewModel.Label(day.DayId, string.Empty)
                : day.Experience is null ? ClosingCellViewModel.Label(day.DayId, "—")
                : participant.GivenDayIds.Contains(day.DayId) ? ClosingCellViewModel.Label(day.DayId, Translate("events.given"))
                : ClosingCellViewModel.Checkbox(day.DayId, tickedByDefault))
            .ToArray();
        return new ClosingRowViewModel(participant.CharacterId, $"#{participant.CharacterNumber} {participant.CharacterName}", baseCell, dayCells);
    }

    /// <summary>Ticks every given checkbox when any is clear; otherwise clears them all.</summary>
    private static void Toggle(IEnumerable<ClosingCellViewModel> cells)
    {
        var checkboxes = cells.Where(cell => cell.IsCheckbox).ToArray();
        var tick = checkboxes.Any(cell => !cell.IsChecked);
        foreach (var cell in checkboxes)
        {
            cell.IsChecked = tick;
        }
    }
}
