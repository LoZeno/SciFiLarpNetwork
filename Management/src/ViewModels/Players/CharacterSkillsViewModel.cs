using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Globalization;
using System.Text.Json;
using System.Text.Json.Nodes;
using SciFiLarpNetwork.Common.Siren;
using SciFiLarpNetwork.Management.Api;
using SciFiLarpNetwork.Management.Api.Players;
using SciFiLarpNetwork.Management.Api.Skills;
using SciFiLarpNetwork.Management.Localization;

namespace SciFiLarpNetwork.Management.ViewModels.Players;

/// <summary>
/// The talents and skills window of one character. Talents and skills are edited here and saved together; free
/// points follow every change. The server checks every rule again when saving.
/// </summary>
public sealed class CharacterSkillsViewModel : ScreenViewModel
{
    private const string SkillsErrorKey = "skills";
    private readonly string _href;
    private readonly AsyncCommand _reloadCommand;
    private SirenEntity? _resource;
    private Dictionary<Guid, TalentProperties> _talentCatalog = [];
    private Dictionary<Guid, SkillProperties> _skillCatalog = [];
    private string _characterName = string.Empty;
    private int _experiencePoints;
    private TalentChoice? _selectedNewTalent;
    private SkillOffer? _selectedNewSkill;
    private string? _affectedSkills;

    internal CharacterSkillsViewModel(
        IManagementApiClient apiClient,
        IManagementLocalizer localizer,
        string apiBaseUrl,
        string locale,
        string href)
        : base(apiClient, localizer, apiBaseUrl, locale)
    {
        _href = href;
        _reloadCommand = new AsyncCommand(LoadAsync, HandleUnexpectedFailure);
        AddTalentCommand = new DelegateCommand(AddTalent, () => CanEdit() && SelectedNewTalent is not null && Talents.Count < TalentLimit);
        AddSkillCommand = new DelegateCommand(AddSkill, () => CanEdit() && SelectedNewSkill is not null);
        SaveCommand = new AsyncCommand(() => RunAsync(SaveAsync), HandleUnexpectedFailure, CanEdit);
        ReloadCommand = new AsyncCommand(LoadAsync, HandleUnexpectedFailure, () => !IsBusy);
        CloseCommand = new DelegateCommand(() => CloseRequested?.Invoke(this, EventArgs.Empty));
        Skills.CollectionChanged += (_, _) => RefreshTotals();
    }

    public event EventHandler? CloseRequested;

    public DelegateCommand AddTalentCommand { get; }

    public DelegateCommand AddSkillCommand { get; }

    public AsyncCommand SaveCommand { get; }

    public AsyncCommand ReloadCommand { get; }

    public DelegateCommand CloseCommand { get; }

    /// <summary>Whether talents and skills were saved at least once while the window was open.</summary>
    public bool IsSaved { get; private set; }

    public ObservableCollection<CharacterTalentRowViewModel> Talents { get; } = [];

    public ObservableCollection<CharacterSkillRowViewModel> Skills { get; } = [];

    /// <summary>Unarchived talents the character does not have yet.</summary>
    public ObservableCollection<TalentChoice> AvailableTalents { get; } = [];

    /// <summary>Unarchived skills the character does not own yet, through each of its talents they belong to.</summary>
    public ObservableCollection<SkillOffer> AvailableSkills { get; } = [];

    public string Title => string.Format(CultureInfo.CurrentCulture, Translate("characterSkills.title"), _characterName);

    /// <summary>The most talents the character can have, as sent by the server.</summary>
    public int TalentLimit { get; private set; }

    public string TalentsLabel => Translate("characterSkills.talents");

    public string TalentLimitText =>
        string.Format(CultureInfo.CurrentCulture, Translate("characterSkills.talentLimit"), TalentLimit);

    public string NewTalentLabel => Translate("characterSkills.newTalent");

    public string AddTalentLabel => Translate("characterSkills.addTalent");

    public string SkillsLabel => Translate("characterSkills.skills");

    public string NewSkillLabel => Translate("characterSkills.newSkill");

    public string AddSkillLabel => Translate("characterSkills.addSkill");

    public string BoughtThroughLabel => Translate("characterSkills.boughtThrough");

    public string CountLabel => Translate("characterSkills.count");

    public string SpecificsLabel => Translate("characterSkills.specifics");

    public string RemoveLabel => Translate("skills.remove");

    public string SaveLabel => Translate("common.save");

    public string ReloadLabel => Translate("common.retry");

    public string CloseLabel => Translate("common.close");

    public string? NoTalentsText => Talents.Count == 0 ? Translate("characterSkills.noTalents") : null;

    public bool HasNoTalents => NoTalentsText is not null;

    public string ExperiencePointsText =>
        string.Format(CultureInfo.CurrentCulture, Translate("characterSkills.experiencePoints"), _experiencePoints);

    /// <summary>
    /// Experience points minus the cost of each skill times its typed purchases; purchases that are not a whole
    /// number of 1 or more count as one.
    /// </summary>
    public long FreePoints => _experiencePoints - Skills.Sum(skill => (long)skill.Cost * (skill.ParsedCount ?? 1));

    public string FreePointsText => string.Format(CultureInfo.CurrentCulture, Translate("characterSkills.freePoints"), FreePoints);

    public bool HasNegativeFreePoints => FreePoints < 0;

    public string NegativeFreePointsText => Translate("characterSkills.negativeFreePoints");

    /// <summary>The skills the last save error is about, or <see langword="null"/>.</summary>
    public string? AffectedSkills
    {
        get => _affectedSkills;
        private set
        {
            if (_affectedSkills == value)
            {
                return;
            }

            _affectedSkills = value;
            OnPropertyChanged();
            OnPropertyChanged(nameof(HasAffectedSkills));
        }
    }

    public bool HasAffectedSkills => AffectedSkills is not null;

    public TalentChoice? SelectedNewTalent
    {
        get => _selectedNewTalent;
        set
        {
            if (_selectedNewTalent == value)
            {
                return;
            }

            _selectedNewTalent = value;
            OnPropertyChanged();
            AddTalentCommand.NotifyCanExecuteChanged();
        }
    }

    public SkillOffer? SelectedNewSkill
    {
        get => _selectedNewSkill;
        set
        {
            if (_selectedNewSkill == value)
            {
                return;
            }

            _selectedNewSkill = value;
            OnPropertyChanged();
            AddSkillCommand.NotifyCanExecuteChanged();
        }
    }

    public override Task LoadAsync() => RunAsync(LoadCoreAsync);

    private protected override void OnLocaleChanged() => _reloadCommand.Execute(null);

    private protected override void NotifyCommandsChanged()
    {
        AddTalentCommand.NotifyCanExecuteChanged();
        AddSkillCommand.NotifyCanExecuteChanged();
        SaveCommand.NotifyCanExecuteChanged();
        ReloadCommand.NotifyCanExecuteChanged();
    }

    private bool CanEdit() => CanWrite && !IsBusy && _resource is not null;

    private async Task LoadCoreAsync()
    {
        var resource = await GetAsync(_href);
        var talents = await GetWithArchivedAsync(resource, SirenRelations.Talents);
        var skills = await GetWithArchivedAsync(resource, SirenRelations.Skills);
        _talentCatalog = (talents.Entities ?? [])
            .Select(entity => SirenProperties.Read<TalentProperties>(entity.Properties))
            .ToDictionary(talent => talent.TalentId);
        _skillCatalog = (skills.Entities ?? [])
            .Select(entity => SirenProperties.Read<SkillProperties>(entity.Properties))
            .ToDictionary(skill => skill.SkillId);
        Apply(resource);
        IsLoaded = true;
    }

    private async Task<SirenEntity> GetWithArchivedAsync(SirenEntity resource, string relation)
    {
        var collection = await GetAsync(resource.FindLinkHref(relation) ?? throw MissingAction());
        return await GetAsync(collection.FindLinkHref(SirenRelations.WithArchived) ?? throw MissingAction());
    }

    private void Apply(SirenEntity resource)
    {
        _resource = resource;
        CanWrite = resource.FindAction("save-character-skills") is not null;
        var saved = SirenProperties.Read<CharacterSkillsProperties>(resource.Properties);
        _characterName = saved.CharacterName;
        _experiencePoints = saved.ExperiencePoints;
        TalentLimit = saved.TalentLimit;
        OnPropertyChanged(nameof(TalentLimitText));
        AffectedSkills = null;
        ReplaceAll(Talents, saved.Talents.Select(talent => NewTalentRow(talent.TalentId, talent.Name, talent.IsArchived)));
        ReplaceAll(Skills, saved.Skills.Select(skill => NewSkillRow(
            skill.SkillId, skill.Name, skill.IsArchived, skill.Cost, skill.Count,
            skill.IsMultiPurchase || skill.Count > 1, skill.Specifics, skill.TalentId)));
        OnPropertyChanged(nameof(Title));
        RefreshChoices();
    }

    private CharacterTalentRowViewModel NewTalentRow(Guid talentId, string name, bool isArchived) =>
        new(talentId, Tagged(name, isArchived), RemoveTalent,
            row => CanEdit() && Skills.All(skill => skill.SelectedTalent?.TalentId != row.TalentId));

    private CharacterSkillRowViewModel NewSkillRow(
        Guid skillId,
        string name,
        bool isArchived,
        int cost,
        int count,
        bool canChangeCount,
        string? specifics,
        Guid talentId)
    {
        var row = new CharacterSkillRowViewModel(
            skillId,
            Tagged(name, isArchived),
            string.Format(CultureInfo.CurrentCulture, Translate("characterSkills.cost"), cost),
            cost,
            count,
            canChangeCount,
            specifics,
            RemoveSkill);
        row.SetTalentChoices([Choice(talentId)]);
        row.PropertyChanged += OnSkillRowChanged;
        return row;
    }

    private void OnSkillRowChanged(object? sender, PropertyChangedEventArgs e)
    {
        if (e.PropertyName == nameof(CharacterSkillRowViewModel.Count))
        {
            RefreshTotals();
        }
        else if (e.PropertyName == nameof(CharacterSkillRowViewModel.SelectedTalent))
        {
            NotifyTalentRemovals();
        }
    }

    private void AddTalent()
    {
        var talent = SelectedNewTalent ?? throw MissingAction();
        Talents.Add(NewTalentRow(talent.TalentId, _talentCatalog[talent.TalentId].Name, isArchived: false));
        RefreshChoices();
    }

    private void RemoveTalent(CharacterTalentRowViewModel row)
    {
        Talents.Remove(row);
        RefreshChoices();
    }

    private void AddSkill()
    {
        var offer = SelectedNewSkill ?? throw MissingAction();
        var skill = _skillCatalog[offer.SkillId];
        Skills.Add(NewSkillRow(skill.SkillId, skill.Name, isArchived: false, skill.Cost, 1, skill.IsMultiPurchase, null, offer.TalentId));
        RefreshChoices();
    }

    private void RemoveSkill(CharacterSkillRowViewModel row)
    {
        Skills.Remove(row);
        RefreshChoices();
    }

    /// <summary>
    /// Rebuilds the talents and skills on offer, and each skill's talent choices: the character's talents the skill
    /// belongs to, plus the talent it is bought through.
    /// </summary>
    private void RefreshChoices()
    {
        var ownedTalents = Talents.Select(talent => talent.TalentId).ToHashSet();
        var ownedSkills = Skills.Select(skill => skill.SkillId).ToHashSet();
        foreach (var row in Skills)
        {
            var current = row.SelectedTalent?.TalentId;
            row.SetTalentChoices(Talents
                .Where(talent => talent.TalentId == current || BelongsTo(row.SkillId, talent.TalentId))
                .Select(talent => Choice(talent.TalentId)));
        }

        ReplaceAll(AvailableTalents, _talentCatalog.Values
            .Where(talent => !talent.IsArchived && !ownedTalents.Contains(talent.TalentId))
            .OrderBy(talent => talent.Name, StringComparer.CurrentCultureIgnoreCase)
            .Select(talent => new TalentChoice(talent.TalentId, talent.Name)));
        ReplaceAll(AvailableSkills, Talents
            .SelectMany(talent => _skillCatalog.Values
                .Where(skill => !skill.IsArchived && !ownedSkills.Contains(skill.SkillId) && skill.TalentIds.Contains(talent.TalentId))
                .Select(skill => Offer(skill, talent.TalentId)))
            .OrderBy(offer => offer.Title, StringComparer.CurrentCultureIgnoreCase));
        SelectedNewTalent = AvailableTalents.FirstOrDefault();
        SelectedNewSkill = AvailableSkills.FirstOrDefault();
        OnPropertyChanged(nameof(NoTalentsText));
        OnPropertyChanged(nameof(HasNoTalents));
        AddTalentCommand.NotifyCanExecuteChanged();
        AddSkillCommand.NotifyCanExecuteChanged();
        NotifyTalentRemovals();
        RefreshTotals();
    }

    private bool BelongsTo(Guid skillId, Guid talentId) =>
        _skillCatalog.TryGetValue(skillId, out var skill) && skill.TalentIds.Contains(talentId);

    private TalentChoice Choice(Guid talentId)
    {
        var name = _talentCatalog.TryGetValue(talentId, out var talent) ? talent.Name : talentId.ToString("D");
        return new TalentChoice(talentId, Tagged(name, talent?.IsArchived ?? false));
    }

    private SkillOffer Offer(SkillProperties skill, Guid talentId) =>
        new(skill.SkillId, talentId, string.Format(CultureInfo.CurrentCulture, Translate("characterSkills.offer"),
            skill.Name, _talentCatalog[talentId].Name, skill.Cost));

    private string Tagged(string name, bool isArchived) =>
        isArchived ? $"{name} {Translate("skills.archivedTag")}" : name;

    private void NotifyTalentRemovals()
    {
        foreach (var talent in Talents)
        {
            talent.RemoveCommand.NotifyCanExecuteChanged();
        }
    }

    private void RefreshTotals()
    {
        OnPropertyChanged(nameof(ExperiencePointsText));
        OnPropertyChanged(nameof(FreePoints));
        OnPropertyChanged(nameof(FreePointsText));
        OnPropertyChanged(nameof(HasNegativeFreePoints));
    }

    private async Task SaveAsync()
    {
        AffectedSkills = null;
        if (Skills.Any(skill => skill.ParsedCount is null))
        {
            StatusMessage = Localizer.TranslateApiError("errors.invalidPurchaseCount", Locale);
            return;
        }

        var resource = _resource ?? throw MissingAction();
        var action = resource.FindAction("save-character-skills") ?? throw MissingAction();
        try
        {
            Apply(await SubmitAsync(action, CreateBody(SirenProperties.Read<CharacterSkillsProperties>(resource.Properties).Version)));
        }
        catch (ManagementApiException failure) when (failure.Errors.TryGetValue(SkillsErrorKey, out var names) && names.Count > 0)
        {
            AffectedSkills = string.Format(CultureInfo.CurrentCulture, Translate("characterSkills.affectedSkills"),
                string.Join(", ", names));
            throw;
        }

        IsSaved = true;
        StatusMessage = Translate("actions.completed");
    }

    private JsonElement CreateBody(long version)
    {
        var skills = new JsonArray();
        foreach (var row in Skills)
        {
            skills.Add(new JsonObject
            {
                ["skillId"] = row.SkillId,
                ["talentId"] = row.SelectedTalent?.TalentId,
                ["count"] = row.ParsedCount,
                ["specifics"] = string.IsNullOrWhiteSpace(row.Specifics) ? null : row.Specifics
            });
        }

        var talentIds = new JsonArray();
        foreach (var talent in Talents)
        {
            talentIds.Add(talent.TalentId);
        }

        return JsonSerializer.SerializeToElement(new JsonObject
        {
            ["version"] = version,
            ["talentIds"] = talentIds,
            ["skills"] = skills
        });
    }
}
