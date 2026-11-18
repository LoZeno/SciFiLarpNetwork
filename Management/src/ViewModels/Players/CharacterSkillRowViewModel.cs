using System.Collections.ObjectModel;
using System.Globalization;

namespace SciFiLarpNetwork.Management.ViewModels.Players;

/// <summary>
/// One skill the character owns: the talent it is bought through, its purchases, and its specifics note. Purchases
/// can be changed for a multi-purchase skill, or lowered for a skill bought more than once before multi-purchase was
/// turned off.
/// </summary>
public sealed class CharacterSkillRowViewModel : ObservableViewModel
{
    private TalentChoice? _selectedTalent;
    private string _count;
    private string _specifics;

    internal CharacterSkillRowViewModel(
        Guid skillId,
        string title,
        string costText,
        int cost,
        int count,
        bool canChangeCount,
        string? specifics,
        Action<CharacterSkillRowViewModel> remove)
    {
        SkillId = skillId;
        Title = title;
        CostText = costText;
        Cost = cost;
        CanChangeCount = canChangeCount;
        _count = count.ToString(CultureInfo.CurrentCulture);
        _specifics = specifics ?? string.Empty;
        RemoveCommand = new DelegateCommand(() => remove(this));
    }

    public Guid SkillId { get; }

    public string Title { get; }

    public string CostText { get; }

    public int Cost { get; }

    public bool CanChangeCount { get; }

    public DelegateCommand RemoveCommand { get; }

    /// <summary>The character's talents the skill can be bought through, and the talent it was bought through.</summary>
    public ObservableCollection<TalentChoice> TalentChoices { get; } = [];

    public TalentChoice? SelectedTalent
    {
        get => _selectedTalent;
        set
        {
            if (_selectedTalent == value)
            {
                return;
            }

            _selectedTalent = value;
            OnPropertyChanged();
        }
    }

    public string Count
    {
        get => _count;
        set
        {
            if (_count == value)
            {
                return;
            }

            _count = value;
            OnPropertyChanged();
        }
    }

    public string Specifics
    {
        get => _specifics;
        set
        {
            if (_specifics == value)
            {
                return;
            }

            _specifics = value;
            OnPropertyChanged();
        }
    }

    /// <summary>The typed purchases, or <see langword="null"/> when they are not a whole number of 1 or more.</summary>
    public int? ParsedCount =>
        int.TryParse(Count.Trim(), NumberStyles.None, CultureInfo.CurrentCulture, out var count) && count >= 1 ? count : null;

    internal void SetTalentChoices(IEnumerable<TalentChoice> choices)
    {
        var selectedId = SelectedTalent?.TalentId;
        TalentChoices.Clear();
        foreach (var choice in choices)
        {
            TalentChoices.Add(choice);
        }

        SelectedTalent = TalentChoices.FirstOrDefault(choice => choice.TalentId == selectedId) ?? TalentChoices.FirstOrDefault();
    }
}
