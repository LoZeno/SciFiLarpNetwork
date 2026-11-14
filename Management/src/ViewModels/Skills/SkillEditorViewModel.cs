using System.Collections.ObjectModel;
using System.Globalization;
using System.Text.Json;
using System.Text.Json.Serialization;
using SciFiLarpNetwork.Management.Localization;

namespace SciFiLarpNetwork.Management.ViewModels.Skills;

public sealed class SkillEditorViewModel : CatalogEditorViewModel
{
    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web)
    {
        DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull
    };

    private readonly Func<SkillEditorViewModel, Task<bool>> _submit;
    private readonly PrerequisiteGroupLabels _groupLabels;
    private decimal? _cost = 0;
    private bool _isMultiPurchase;
    private bool _isAdvanced;
    private string _newCardFlagNames = string.Empty;
    private string? _archivedPrerequisiteWarning;

    internal SkillEditorViewModel(
        IManagementLocalizer localizer,
        string locale,
        string titleKey,
        SkillEditorChoices choices,
        Func<SkillEditorViewModel, Task<bool>> submit)
        : base(localizer, locale, titleKey)
    {
        _submit = submit;
        Talents = choices.Talents;
        GameFlags = choices.GameFlags;
        CardFlags = choices.CardFlags;
        PrerequisiteCandidates = choices.PrerequisiteCandidates;
        _groupLabels = new PrerequisiteGroupLabels(
            Translate("skills.prerequisiteGroup"),
            Translate("skills.addSkill"),
            Translate("skills.removeGroup"),
            Translate("skills.remove"));
        AddGroupCommand = new DelegateCommand(() => AddGroup([]));
    }

    public IReadOnlyList<SelectableOption> Talents { get; }

    public IReadOnlyList<SelectableOption> GameFlags { get; }

    public IReadOnlyList<SelectableOption> CardFlags { get; }

    public IReadOnlyList<SkillListItem> PrerequisiteCandidates { get; }

    public ObservableCollection<PrerequisiteGroupViewModel> PrerequisiteGroups { get; } = [];

    public DelegateCommand AddGroupCommand { get; }

    public string CostLabel => Translate("skills.cost");

    public string MultiPurchaseLabel => Translate("skills.multiPurchase");

    public string AdvancedLabel => Translate("skills.advanced");

    public string TalentsLabel => Translate("skills.talents");

    public string GameFlagsLabel => Translate("skills.gameFlags");

    public string CardFlagsLabel => Translate("skills.cardFlags");

    public string NewCardFlagsLabel => Translate("skills.newCardFlags");

    public string NewCardFlagsHelp => Translate("skills.newCardFlagsHelp");

    public string PrerequisitesLabel => Translate("skills.prerequisites");

    public string PrerequisitesHelp => Translate("skills.prerequisitesHelp");

    public string AddGroupLabel => Translate("skills.addGroup");

    public decimal? Cost
    {
        get => _cost;
        set
        {
            if (_cost == value)
            {
                return;
            }

            _cost = value;
            OnPropertyChanged();
        }
    }

    public bool IsMultiPurchase
    {
        get => _isMultiPurchase;
        set
        {
            if (_isMultiPurchase == value)
            {
                return;
            }

            _isMultiPurchase = value;
            OnPropertyChanged();
        }
    }

    public bool IsAdvanced
    {
        get => _isAdvanced;
        set
        {
            if (_isAdvanced == value)
            {
                return;
            }

            _isAdvanced = value;
            OnPropertyChanged();
        }
    }

    /// <summary>New card flag names, one per line.</summary>
    public string NewCardFlagNames
    {
        get => _newCardFlagNames;
        set
        {
            if (_newCardFlagNames == value)
            {
                return;
            }

            _newCardFlagNames = value;
            OnPropertyChanged();
        }
    }

    public string? ArchivedPrerequisiteWarning
    {
        get => _archivedPrerequisiteWarning;
        private set
        {
            if (_archivedPrerequisiteWarning == value)
            {
                return;
            }

            _archivedPrerequisiteWarning = value;
            OnPropertyChanged();
            OnPropertyChanged(nameof(HasArchivedPrerequisiteWarning));
        }
    }

    public bool HasArchivedPrerequisiteWarning => ArchivedPrerequisiteWarning is not null;

    internal void AddGroup(IEnumerable<SkillListItem> members)
    {
        var group = new PrerequisiteGroupViewModel(PrerequisiteCandidates, _groupLabels, RemoveGroup, OnPrerequisitesChanged);
        PrerequisiteGroups.Add(group);
        foreach (var member in members)
        {
            group.AddMember(member);
        }

        OnPrerequisitesChanged();
    }

    internal void ClearPrerequisiteGroups()
    {
        PrerequisiteGroups.Clear();
        OnPrerequisitesChanged();
    }

    internal bool TryCreateRequestBody(long? version, out JsonElement body)
    {
        if (Cost is not { } cost || cost != decimal.Truncate(cost) || cost < int.MinValue || cost > int.MaxValue)
        {
            ErrorMessage = Localizer.TranslateApiError("errors.invalidCost", Locale);
            body = default;
            return false;
        }

        body = JsonSerializer.SerializeToElement(
            new
            {
                version,
                name = Name,
                description = string.IsNullOrWhiteSpace(Description) ? null : Description,
                cost = (int)cost,
                isMultiPurchase = IsMultiPurchase,
                isAdvanced = IsAdvanced,
                talentIds = SelectedValues(Talents),
                gameFlags = SelectedValues(GameFlags),
                cardFlagIds = SelectedValues(CardFlags),
                newCardFlagNames = NewCardFlagNames
                    .Split(['\r', '\n'], StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries),
                prerequisiteGroups = PrerequisiteGroups
                    .Select(group => group.Members.Select(member => member.Skill.SkillId).ToArray())
                    .ToArray()
            },
            JsonOptions);
        return true;
    }

    private protected override Task<bool> SubmitAsync() => _submit(this);

    private static string[] SelectedValues(IEnumerable<SelectableOption> options) =>
        options.Where(option => option.IsSelected).Select(option => option.Value).ToArray();

    private void RemoveGroup(PrerequisiteGroupViewModel group)
    {
        PrerequisiteGroups.Remove(group);
        OnPrerequisitesChanged();
    }

    private void OnPrerequisitesChanged()
    {
        for (var index = 0; index < PrerequisiteGroups.Count; index++)
        {
            PrerequisiteGroups[index].Number = index + 1;
        }

        var archivedNames = PrerequisiteGroups
            .SelectMany(group => group.Members)
            .Where(member => member.Skill.IsArchived)
            .Select(member => member.Skill.Name)
            .Distinct(StringComparer.Ordinal)
            .ToArray();
        ArchivedPrerequisiteWarning = archivedNames.Length == 0
            ? null
            : string.Format(CultureInfo.CurrentCulture, Translate("skills.archivedPrerequisites"), string.Join(", ", archivedNames));
    }
}
