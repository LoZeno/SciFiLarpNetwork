using System.Collections.ObjectModel;
using System.Globalization;
using System.Text.Json;
using SciFiLarpNetwork.Common.Siren;
using SciFiLarpNetwork.Management.Api;
using SciFiLarpNetwork.Management.Api.Players;
using SciFiLarpNetwork.Management.Api.Skills;
using SciFiLarpNetwork.Management.Localization;

namespace SciFiLarpNetwork.Management.ViewModels.Players;

/// <summary>
/// The stats window: lists character stats, and lets administrators rename, archive, and unarchive them. New stats
/// are typed in the character dialog.
/// </summary>
public sealed class CharacterStatsViewModel : ScreenViewModel
{
    private readonly IPlayerDialogService _dialogs;
    private readonly string _statsHref;
    private readonly IReadOnlyDictionary<string, string> _typeTitles;
    private readonly AsyncCommand _reloadCommand;
    private CharacterStatListItem? _selectedStat;
    private string _newName = string.Empty;
    private bool _showArchived;

    internal CharacterStatsViewModel(
        IManagementApiClient apiClient,
        IManagementLocalizer localizer,
        IPlayerDialogService dialogs,
        string apiBaseUrl,
        string locale,
        string statsHref,
        IReadOnlyDictionary<string, string> typeTitles,
        bool canWrite)
        : base(apiClient, localizer, apiBaseUrl, locale)
    {
        _dialogs = dialogs;
        _statsHref = statsHref;
        _typeTitles = typeTitles;
        _reloadCommand = new AsyncCommand(LoadAsync, HandleUnexpectedFailure);
        RenameCommand = new AsyncCommand(() => RunAsync(RenameAsync), HandleUnexpectedFailure,
            () => CanChangeSelected() && !string.IsNullOrWhiteSpace(NewName));
        ToggleArchivedCommand = new AsyncCommand(() => RunAsync(ToggleArchivedAsync), HandleUnexpectedFailure, CanChangeSelected);
        CloseCommand = new DelegateCommand(() => CloseRequested?.Invoke(this, EventArgs.Empty));
        CanWrite = canWrite;
    }

    public event EventHandler? CloseRequested;

    public AsyncCommand RenameCommand { get; }

    public AsyncCommand ToggleArchivedCommand { get; }

    public DelegateCommand CloseCommand { get; }

    public ObservableCollection<CharacterStatListItem> Stats { get; } = [];

    public string Title => Translate("stats.stats");

    public string Help => Translate("stats.help");

    public string ShowArchivedLabel => Translate("stats.showArchived");

    public string NewNameLabel => Translate("stats.newName");

    public string RenameLabel => Translate("stats.rename");

    public string CloseLabel => Translate("common.close");

    public string ToggleArchivedLabel => Translate(SelectedStat is { IsArchived: true } ? "skills.unarchive" : "skills.archive");

    public string? StatsPlaceholder => Stats.Count == 0 ? Translate("common.noResults") : null;

    public bool HasStatsPlaceholder => StatsPlaceholder is not null;

    public CharacterStatListItem? SelectedStat
    {
        get => _selectedStat;
        set
        {
            if (_selectedStat == value)
            {
                return;
            }

            _selectedStat = value;
            OnPropertyChanged();
            OnPropertyChanged(nameof(ToggleArchivedLabel));
            NewName = value?.Name ?? string.Empty;
            NotifyCommandsChanged();
        }
    }

    public string NewName
    {
        get => _newName;
        set
        {
            if (_newName == value)
            {
                return;
            }

            _newName = value;
            OnPropertyChanged();
            RenameCommand.NotifyCanExecuteChanged();
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

    public override Task LoadAsync() => RunAsync(LoadStatsCoreAsync);

    private protected override void OnLocaleChanged() => _reloadCommand.Execute(null);

    private protected override void NotifyCommandsChanged()
    {
        RenameCommand.NotifyCanExecuteChanged();
        ToggleArchivedCommand.NotifyCanExecuteChanged();
    }

    private async Task LoadStatsCoreAsync()
    {
        var collection = await GetAsync(_statsHref);
        if (ShowArchived)
        {
            collection = await GetAsync(collection.FindLinkHref(SirenRelations.WithArchived) ?? throw MissingAction());
        }

        var selectedId = SelectedStat?.StatId;
        ReplaceAll(Stats, (collection.Entities ?? []).Select(ToListItem));
        IsLoaded = true;
        SelectedStat = Stats.FirstOrDefault(stat => stat.StatId == selectedId);
        OnPropertyChanged(nameof(StatsPlaceholder));
        OnPropertyChanged(nameof(HasStatsPlaceholder));
    }

    private async Task RenameAsync()
    {
        var stat = SelectedStat ?? throw MissingAction();
        var resource = await GetAsync(stat.Href);
        var action = resource.FindAction("rename-character-stat") ?? throw MissingAction();
        var version = SirenProperties.Read<CharacterStatProperties>(resource.Properties).Version;
        await SubmitAsync(action, JsonSerializer.SerializeToElement(new { version, name = NewName }));
        await LoadStatsCoreAsync();
        StatusMessage = Translate("actions.completed");
    }

    private async Task ToggleArchivedAsync()
    {
        var stat = SelectedStat ?? throw MissingAction();
        var resource = await GetAsync(stat.Href);
        var verb = stat.IsArchived ? "unarchive" : "archive";
        var action = resource.FindAction($"{verb}-character-stat") ?? throw MissingAction();
        var confirmed = await _dialogs.ConfirmAsync(new ConfirmationRequest(
            Translate("skills.confirmTitle"),
            string.Format(CultureInfo.CurrentCulture, Translate($"stats.confirm{(stat.IsArchived ? "Unarchive" : "Archive")}"), stat.Name),
            Translate($"skills.{verb}"),
            Translate("common.cancel")));
        if (!confirmed)
        {
            return;
        }

        await SubmitAsync(action, null);
        await LoadStatsCoreAsync();
        StatusMessage = Translate("actions.completed");
    }

    private CharacterStatListItem ToListItem(SirenSubEntity entity)
    {
        var stat = SirenProperties.Read<CharacterStatProperties>(entity.Properties);
        var type = _typeTitles.GetValueOrDefault(stat.Type, stat.Type);
        var archived = stat.IsArchived ? $" {Translate("stats.archivedTag")}" : string.Empty;
        return new CharacterStatListItem(
            stat.StatId,
            stat.Name,
            stat.IsArchived,
            entity.Href ?? throw MissingAction(),
            $"{stat.Name} ({type}){archived}");
    }

    private bool CanChangeSelected() => CanWrite && !IsBusy && SelectedStat is not null;
}
