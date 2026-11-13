using System.Collections.ObjectModel;
using System.Diagnostics;
using System.Globalization;
using System.Net;
using System.Text.Json;
using SciFiLarpNetwork.Common.Siren;
using SciFiLarpNetwork.Management.Api;
using SciFiLarpNetwork.Management.Api.Skills;
using SciFiLarpNetwork.Management.Localization;

namespace SciFiLarpNetwork.Management.ViewModels.Skills;

/// <summary>
/// The talents and skills screen. It follows only the links and actions the API advertises, so readers see
/// no change commands and the server still authorizes and validates every change.
/// </summary>
public sealed class TalentCatalogViewModel : ObservableViewModel
{
    private const string StaleVersionCode = "errors.staleVersion";
    private readonly IManagementApiClient _apiClient;
    private readonly IManagementLocalizer _localizer;
    private readonly ICatalogDialogService _dialogs;
    private readonly string _apiBaseUrl;
    private readonly string _talentsHref;
    private readonly AsyncCommand _loadSkillsCommand;
    private readonly AsyncCommand _reloadCommand;
    private string _locale;
    private SirenEntity? _talentCollection;
    private TalentListItem? _selectedTalent;
    private SkillListItem? _selectedSkill;
    private bool _showArchivedTalents;
    private bool _showArchivedSkills;
    private bool _isBusy;
    private bool _canWrite;
    private string? _statusMessage;
    private int _skillsLoadGeneration;

    public TalentCatalogViewModel(
        IManagementApiClient apiClient,
        IManagementLocalizer localizer,
        ICatalogDialogService dialogs,
        string apiBaseUrl,
        string locale,
        string talentsHref)
    {
        _apiClient = apiClient;
        _localizer = localizer;
        _dialogs = dialogs;
        _apiBaseUrl = apiBaseUrl;
        _locale = locale;
        _talentsHref = talentsHref;
        _loadSkillsCommand = new AsyncCommand(() => RunAsync(LoadSkillsCoreAsync), HandleUnexpectedFailure);
        _reloadCommand = new AsyncCommand(LoadAsync, HandleUnexpectedFailure);
        NewTalentCommand = new AsyncCommand(() => RunAsync(CreateTalentAsync), HandleUnexpectedFailure, () => CanWrite && !IsBusy);
        EditTalentCommand = new AsyncCommand(() => RunAsync(EditTalentAsync), HandleUnexpectedFailure, CanChangeSelectedTalent);
        ToggleTalentArchiveCommand = new AsyncCommand(() => RunAsync(ToggleTalentArchiveAsync), HandleUnexpectedFailure, CanChangeSelectedTalent);
        NewSkillCommand = new AsyncCommand(() => RunAsync(CreateSkillAsync), HandleUnexpectedFailure, () => CanWrite && !IsBusy);
        EditSkillCommand = new AsyncCommand(() => RunAsync(EditSkillAsync), HandleUnexpectedFailure, CanChangeSelectedSkill);
        ToggleSkillArchiveCommand = new AsyncCommand(() => RunAsync(ToggleSkillArchiveAsync), HandleUnexpectedFailure, CanChangeSelectedSkill);
    }

    /// <summary>Raised when the API rejects the session, so the shell can clear its signed-in state.</summary>
    public event EventHandler? SessionExpired;

    public AsyncCommand NewTalentCommand { get; }

    public AsyncCommand EditTalentCommand { get; }

    public AsyncCommand ToggleTalentArchiveCommand { get; }

    public AsyncCommand NewSkillCommand { get; }

    public AsyncCommand EditSkillCommand { get; }

    public AsyncCommand ToggleSkillArchiveCommand { get; }

    public ObservableCollection<TalentListItem> Talents { get; } = [];

    public ObservableCollection<SkillListItem> Skills { get; } = [];

    public string Locale
    {
        get => _locale;
        set
        {
            if (_locale == value)
            {
                return;
            }

            _locale = value;
            OnPropertyChanged();
            OnLabelsChanged();
            _reloadCommand.Execute(null);
        }
    }

    public TalentListItem? SelectedTalent
    {
        get => _selectedTalent;
        set
        {
            if (_selectedTalent == value)
            {
                return;
            }

            SetSelectedTalent(value);
            SelectedSkill = null;
            _loadSkillsCommand.Execute(null);
        }
    }

    public SkillListItem? SelectedSkill
    {
        get => _selectedSkill;
        set
        {
            if (_selectedSkill == value)
            {
                return;
            }

            _selectedSkill = value;
            OnPropertyChanged();
            OnPropertyChanged(nameof(SkillArchiveLabel));
            EditSkillCommand.NotifyCanExecuteChanged();
            ToggleSkillArchiveCommand.NotifyCanExecuteChanged();
        }
    }

    public bool ShowArchivedTalents
    {
        get => _showArchivedTalents;
        set
        {
            if (_showArchivedTalents == value)
            {
                return;
            }

            _showArchivedTalents = value;
            OnPropertyChanged();
            _reloadCommand.Execute(null);
        }
    }

    public bool ShowArchivedSkills
    {
        get => _showArchivedSkills;
        set
        {
            if (_showArchivedSkills == value)
            {
                return;
            }

            _showArchivedSkills = value;
            OnPropertyChanged();
            _loadSkillsCommand.Execute(null);
        }
    }

    public bool CanWrite
    {
        get => _canWrite;
        private set
        {
            if (_canWrite == value)
            {
                return;
            }

            _canWrite = value;
            OnPropertyChanged();
            NotifyCommandsChanged();
        }
    }

    public bool IsBusy
    {
        get => _isBusy;
        private set
        {
            if (_isBusy == value)
            {
                return;
            }

            _isBusy = value;
            OnPropertyChanged();
            NotifyCommandsChanged();
        }
    }

    public string? StatusMessage
    {
        get => _statusMessage;
        private set
        {
            if (_statusMessage == value)
            {
                return;
            }

            _statusMessage = value;
            OnPropertyChanged();
            OnPropertyChanged(nameof(HasStatusMessage));
        }
    }

    public bool HasStatusMessage => !string.IsNullOrEmpty(StatusMessage);

    public string TalentsHeading => Translate("skills.talents");

    public string SkillsHeading => Translate("skills.skills");

    public string ShowArchivedLabel => Translate("skills.showArchived");

    public string NewLabel => Translate("skills.new");

    public string EditLabel => Translate("skills.edit");

    public string TalentArchiveLabel => Translate(SelectedTalent?.IsArchived == true ? "skills.unarchive" : "skills.archive");

    public string SkillArchiveLabel => Translate(SelectedSkill?.IsArchived == true ? "skills.unarchive" : "skills.archive");

    public string? TalentsPlaceholder => Talents.Count == 0 ? Translate("common.noResults") : null;

    public string? SkillsPlaceholder => SelectedTalent is null
        ? Translate("skills.selectTalent")
        : Skills.Count == 0 ? Translate("common.noResults") : null;

    public bool HasTalentsPlaceholder => TalentsPlaceholder is not null;

    public bool HasSkillsPlaceholder => SkillsPlaceholder is not null;

    public Task LoadAsync() => RunAsync(LoadTalentsCoreAsync);

    private async Task LoadTalentsCoreAsync()
    {
        var collection = await GetCollectionAsync(_talentsHref, ShowArchivedTalents);
        _talentCollection = collection;
        CanWrite = collection.FindAction("create-talent") is not null;
        var items = (collection.Entities ?? []).Select(ToTalentItem).ToArray();
        var selectedId = SelectedTalent?.TalentId;
        ReplaceAll(Talents, items);
        OnPropertyChanged(nameof(TalentsPlaceholder));
        OnPropertyChanged(nameof(HasTalentsPlaceholder));
        SetSelectedTalent(items.FirstOrDefault(item => item.TalentId == selectedId));
        await LoadSkillsCoreAsync();
    }

    private async Task LoadSkillsCoreAsync()
    {
        var generation = ++_skillsLoadGeneration;
        SkillListItem[] items = [];
        if (SelectedTalent is { } talent)
        {
            var collection = await GetCollectionAsync(talent.SkillsHref, ShowArchivedSkills);
            if (generation != _skillsLoadGeneration)
            {
                return;
            }

            items = (collection.Entities ?? []).Select(ToSkillItem).ToArray();
        }

        var selectedId = SelectedSkill?.SkillId;
        ReplaceAll(Skills, items);
        SelectedSkill = items.FirstOrDefault(item => item.SkillId == selectedId);
        OnPropertyChanged(nameof(SkillsPlaceholder));
        OnPropertyChanged(nameof(HasSkillsPlaceholder));
    }

    private async Task CreateTalentAsync()
    {
        var action = _talentCollection?.FindAction("create-talent") ?? throw MissingAction();
        var editor = new TalentEditorViewModel(
            _localizer,
            Locale,
            "skills.newTalent",
            editor => SubmitEditorAsync(editor, () => SubmitAsync(action, editor.CreateRequestBody(null)), null));
        await _dialogs.ShowTalentEditorAsync(editor);
        await ReloadAfterSaveAsync(editor);
    }

    private async Task EditTalentAsync()
    {
        var href = SelectedTalent?.Href ?? throw MissingAction();
        var resource = await GetAsync(href);
        var action = resource.FindAction("update-talent") ?? throw MissingAction();
        var talent = SirenProperties.Read<TalentProperties>(resource.Properties);
        var version = talent.Version;
        var editor = new TalentEditorViewModel(
            _localizer,
            Locale,
            "skills.editTalent",
            editor => SubmitEditorAsync(
                editor,
                () => SubmitAsync(action, editor.CreateRequestBody(version)),
                async () =>
                {
                    var latest = SirenProperties.Read<TalentProperties>((await GetAsync(href)).Properties);
                    version = latest.Version;
                    ApplyTalent(editor, latest);
                }));
        ApplyTalent(editor, talent);
        await _dialogs.ShowTalentEditorAsync(editor);
        await ReloadAfterSaveAsync(editor);
    }

    private async Task CreateSkillAsync()
    {
        Guid[] defaultTalents = SelectedTalent is { IsArchived: false } talent ? [talent.TalentId] : [];
        var choices = await LoadSkillChoicesAsync(null, defaultTalents);
        var action = choices.AllSkills.FindAction("create-skill") ?? throw MissingAction();
        var editor = new SkillEditorViewModel(
            _localizer,
            Locale,
            "skills.newSkill",
            choices.Choices,
            editor => SubmitSkillAsync(editor, action, () => null, null));
        await _dialogs.ShowSkillEditorAsync(editor);
        await ReloadAfterSaveAsync(editor);
    }

    private async Task EditSkillAsync()
    {
        var href = SelectedSkill?.Href ?? throw MissingAction();
        var resource = await GetAsync(href);
        var action = resource.FindAction("update-skill") ?? throw MissingAction();
        var skill = SirenProperties.Read<SkillProperties>(resource.Properties);
        var version = skill.Version;
        var choices = await LoadSkillChoicesAsync(skill, skill.TalentIds);
        var editor = new SkillEditorViewModel(
            _localizer,
            Locale,
            "skills.editSkill",
            choices.Choices,
            editor => SubmitSkillAsync(
                editor,
                action,
                () => version,
                async () =>
                {
                    var latest = SirenProperties.Read<SkillProperties>((await GetAsync(href)).Properties);
                    version = latest.Version;
                    ApplySkill(editor, latest);
                }));
        ApplySkill(editor, skill);
        await _dialogs.ShowSkillEditorAsync(editor);
        await ReloadAfterSaveAsync(editor);
    }

    private Task<bool> SubmitSkillAsync(
        SkillEditorViewModel editor,
        SirenAction action,
        Func<long?> version,
        Func<Task>? reloadAfterConflict) =>
        editor.TryCreateRequestBody(version(), out var body)
            ? SubmitEditorAsync(editor, () => SubmitAsync(action, body), reloadAfterConflict)
            : Task.FromResult(false);

    private async Task ToggleTalentArchiveAsync()
    {
        var href = SelectedTalent?.Href ?? throw MissingAction();
        var resource = await GetAsync(href);
        var talent = SirenProperties.Read<TalentProperties>(resource.Properties);
        await ToggleArchiveAsync(resource, "talent", talent.Name, talent.IsArchived);
    }

    private async Task ToggleSkillArchiveAsync()
    {
        var href = SelectedSkill?.Href ?? throw MissingAction();
        var resource = await GetAsync(href);
        var skill = SirenProperties.Read<SkillProperties>(resource.Properties);
        await ToggleArchiveAsync(resource, "skill", skill.Name, skill.IsArchived);
    }

    private async Task ToggleArchiveAsync(SirenEntity resource, string kind, string name, bool isArchived)
    {
        var verb = isArchived ? "unarchive" : "archive";
        var action = resource.FindAction($"{verb}-{kind}") ?? throw MissingAction();
        var messageKey = $"skills.confirm{char.ToUpperInvariant(verb[0])}{verb[1..]}{char.ToUpperInvariant(kind[0])}{kind[1..]}";
        var confirmed = await _dialogs.ConfirmAsync(new ConfirmationRequest(
            Translate("skills.confirmTitle"),
            string.Format(CultureInfo.CurrentCulture, Translate(messageKey), name),
            Translate($"skills.{verb}"),
            Translate("common.cancel")));
        if (!confirmed)
        {
            return;
        }

        await SubmitAsync(action, null);
        await LoadTalentsCoreAsync();
        StatusMessage = Translate("actions.completed");
    }

    private async Task<(SirenEntity AllSkills, SkillEditorChoices Choices)> LoadSkillChoicesAsync(
        SkillProperties? skill,
        IReadOnlyCollection<Guid> assignedTalentIds)
    {
        var talents = await GetCollectionAsync(_talentsHref, includeArchived: true);
        var allSkills = await GetCollectionAsync(talents.FindLinkHref(SirenRelations.Skills) ?? throw MissingAction(), includeArchived: true);
        var cardFlags = SirenProperties.Read<CardFlagCollectionProperties>(
            (await GetAsync(allSkills.FindLinkHref(SirenRelations.CardFlags) ?? throw MissingAction())).Properties).CardFlags;
        var gameFlags = SirenProperties.Read<SkillCollectionProperties>(allSkills.Properties).GameFlags;
        var talentOptions = (talents.Entities ?? [])
            .Select(entity => SirenProperties.Read<TalentProperties>(entity.Properties))
            .Where(talent => !talent.IsArchived || assignedTalentIds.Contains(talent.TalentId))
            .Select(talent => new SelectableOption(
                talent.TalentId.ToString("D"),
                DisplayName(talent.Name, talent.IsArchived),
                assignedTalentIds.Contains(talent.TalentId)))
            .ToArray();
        var candidates = (allSkills.Entities ?? [])
            .Select(ToSkillItem)
            .Where(candidate => candidate.SkillId != skill?.SkillId)
            .ToArray();
        return (allSkills, new SkillEditorChoices(
            talentOptions,
            gameFlags.Select(flag => new SelectableOption(flag.Name, flag.Title)).ToArray(),
            cardFlags.Select(flag => new SelectableOption(flag.CardFlagId.ToString("D"), flag.Name)).ToArray(),
            candidates));
    }

    private static void ApplyTalent(TalentEditorViewModel editor, TalentProperties talent)
    {
        editor.Name = talent.Name;
        editor.Description = talent.Description ?? string.Empty;
    }

    private static void ApplySkill(SkillEditorViewModel editor, SkillProperties skill)
    {
        editor.Name = skill.Name;
        editor.Description = skill.Description ?? string.Empty;
        editor.Cost = skill.Cost;
        editor.IsMultiPurchase = skill.IsMultiPurchase;
        editor.IsAdvanced = skill.IsAdvanced;
        editor.NewCardFlagNames = string.Empty;
        SelectOnly(editor.Talents, skill.TalentIds.Select(id => id.ToString("D")));
        SelectOnly(editor.GameFlags, skill.GameFlags);
        SelectOnly(editor.CardFlags, skill.CardFlagIds.Select(id => id.ToString("D")));
        editor.ClearPrerequisiteGroups();
        foreach (var group in skill.PrerequisiteGroups)
        {
            editor.AddGroup(group
                .Select(skillId => editor.PrerequisiteCandidates.FirstOrDefault(candidate => candidate.SkillId == skillId))
                .OfType<SkillListItem>());
        }
    }

    private static void SelectOnly(IEnumerable<SelectableOption> options, IEnumerable<string> values)
    {
        var selected = values.ToHashSet(StringComparer.Ordinal);
        foreach (var option in options)
        {
            option.IsSelected = selected.Contains(option.Value);
        }
    }

    private async Task<bool> SubmitEditorAsync(
        CatalogEditorViewModel editor,
        Func<Task> submit,
        Func<Task>? reloadAfterConflict)
    {
        try
        {
            await submit();
            return true;
        }
        catch (Exception failure) when (IsExpectedFailure(failure))
        {
            var message = Report(failure);
            if (failure is ManagementApiException { ErrorCode: StaleVersionCode } && reloadAfterConflict is not null)
            {
                message = await TryReloadAfterConflictAsync(reloadAfterConflict) ?? message;
            }

            editor.ErrorMessage = message;
            return false;
        }
    }

    private async Task<string?> TryReloadAfterConflictAsync(Func<Task> reload)
    {
        try
        {
            await reload();
            return null;
        }
        catch (Exception failure) when (IsExpectedFailure(failure))
        {
            return Report(failure);
        }
    }

    private async Task ReloadAfterSaveAsync(CatalogEditorViewModel editor)
    {
        if (!editor.IsSaved)
        {
            return;
        }

        await LoadTalentsCoreAsync();
        StatusMessage = Translate("actions.completed");
    }

    private async Task<SirenEntity> GetCollectionAsync(string href, bool includeArchived)
    {
        var collection = await GetAsync(href);
        return includeArchived
            ? await GetAsync(collection.FindLinkHref(SirenRelations.WithArchived) ?? throw MissingAction())
            : collection;
    }

    private Task<SirenEntity> GetAsync(string href) =>
        _apiClient.GetResourceAsync(_apiBaseUrl, href, Locale);

    private Task<SirenEntity> SubmitAsync(SirenAction action, JsonElement? body) =>
        _apiClient.SubmitActionAsync(_apiBaseUrl, action, body, Locale);

    private TalentListItem ToTalentItem(SirenSubEntity entity)
    {
        var talent = SirenProperties.Read<TalentProperties>(entity.Properties);
        return new TalentListItem(
            talent.TalentId,
            talent.Name,
            talent.IsArchived,
            entity.Href ?? throw MissingAction(),
            entity.FindLinkHref(SirenRelations.Skills) ?? throw MissingAction(),
            DisplayName(talent.Name, talent.IsArchived));
    }

    private SkillListItem ToSkillItem(SirenSubEntity entity)
    {
        var skill = SirenProperties.Read<SkillProperties>(entity.Properties);
        return new SkillListItem(
            skill.SkillId,
            skill.Name,
            skill.IsArchived,
            entity.Href ?? throw MissingAction(),
            DisplayName(skill.Name, skill.IsArchived));
    }

    private string DisplayName(string name, bool isArchived) =>
        isArchived ? $"{name} {Translate("skills.archivedTag")}" : name;

    private static void ReplaceAll<T>(ObservableCollection<T> collection, IEnumerable<T> items)
    {
        collection.Clear();
        foreach (var item in items)
        {
            collection.Add(item);
        }
    }

    private void SetSelectedTalent(TalentListItem? talent)
    {
        _selectedTalent = talent;
        OnPropertyChanged(nameof(SelectedTalent));
        OnPropertyChanged(nameof(TalentArchiveLabel));
        OnPropertyChanged(nameof(SkillsPlaceholder));
        OnPropertyChanged(nameof(HasSkillsPlaceholder));
        EditTalentCommand.NotifyCanExecuteChanged();
        ToggleTalentArchiveCommand.NotifyCanExecuteChanged();
    }

    private bool CanChangeSelectedTalent() => CanWrite && !IsBusy && SelectedTalent is not null;

    private bool CanChangeSelectedSkill() => CanWrite && !IsBusy && SelectedSkill is not null;

    private async Task RunAsync(Func<Task> operation)
    {
        IsBusy = true;
        StatusMessage = null;
        try
        {
            await operation();
        }
        catch (Exception failure) when (IsExpectedFailure(failure))
        {
            StatusMessage = Report(failure);
        }
        finally
        {
            IsBusy = false;
        }
    }

    private static bool IsExpectedFailure(Exception failure) =>
        failure is ManagementApiException or HttpRequestException or JsonException or InvalidOperationException
            or UriFormatException;

    private string Report(Exception failure)
    {
        switch (failure)
        {
            case ManagementApiException apiFailure:
                if (apiFailure.StatusCode == HttpStatusCode.Unauthorized)
                {
                    SessionExpired?.Invoke(this, EventArgs.Empty);
                }

                return _localizer.TranslateApiError(apiFailure.ErrorCode, Locale);
            case HttpRequestException:
                return Translate("app.connectionError");
            case UriFormatException:
                return Translate("validation.invalid");
            case InvalidOperationException:
                return Translate("actions.unsupported");
            default:
                return Translate("common.error");
        }
    }

    private static InvalidOperationException MissingAction() =>
        new("The API did not advertise the link or action this screen needs.");

    private void HandleUnexpectedFailure(Exception exception)
    {
        Trace.TraceError("Unhandled talent catalog exception: {0}", exception);
        IsBusy = false;
        StatusMessage = Translate("common.error");
    }

    private string Translate(string key) => _localizer.Translate(key, Locale);

    private void NotifyCommandsChanged()
    {
        NewTalentCommand.NotifyCanExecuteChanged();
        EditTalentCommand.NotifyCanExecuteChanged();
        ToggleTalentArchiveCommand.NotifyCanExecuteChanged();
        NewSkillCommand.NotifyCanExecuteChanged();
        EditSkillCommand.NotifyCanExecuteChanged();
        ToggleSkillArchiveCommand.NotifyCanExecuteChanged();
    }

    private void OnLabelsChanged()
    {
        OnPropertyChanged(nameof(TalentsHeading));
        OnPropertyChanged(nameof(SkillsHeading));
        OnPropertyChanged(nameof(ShowArchivedLabel));
        OnPropertyChanged(nameof(NewLabel));
        OnPropertyChanged(nameof(EditLabel));
        OnPropertyChanged(nameof(TalentArchiveLabel));
        OnPropertyChanged(nameof(SkillArchiveLabel));
        OnPropertyChanged(nameof(TalentsPlaceholder));
        OnPropertyChanged(nameof(SkillsPlaceholder));
    }
}
