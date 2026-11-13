using System.Collections.ObjectModel;
using System.Globalization;

namespace SciFiLarpNetwork.Management.ViewModels.Skills;

/// <summary>
/// One requirement group: a character meets it by owning any one of its skills.
/// </summary>
public sealed class PrerequisiteGroupViewModel : ObservableViewModel
{
    private readonly PrerequisiteGroupLabels _labels;
    private readonly Action _changed;
    private SkillListItem? _selectedCandidate;
    private int _number;

    internal PrerequisiteGroupViewModel(
        IReadOnlyList<SkillListItem> candidates,
        PrerequisiteGroupLabels labels,
        Action<PrerequisiteGroupViewModel> remove,
        Action changed)
    {
        Candidates = candidates;
        _labels = labels;
        _changed = changed;
        AddMemberCommand = new DelegateCommand(AddSelectedCandidate, CanAddSelectedCandidate);
        RemoveCommand = new DelegateCommand(() => remove(this));
    }

    public ObservableCollection<PrerequisiteMemberViewModel> Members { get; } = [];

    public IReadOnlyList<SkillListItem> Candidates { get; }

    public DelegateCommand AddMemberCommand { get; }

    public DelegateCommand RemoveCommand { get; }

    public string Heading => string.Format(CultureInfo.CurrentCulture, _labels.HeadingFormat, Number);

    public string AddMemberLabel => _labels.AddMember;

    public string RemoveLabel => _labels.RemoveGroup;

    public int Number
    {
        get => _number;
        internal set
        {
            if (_number == value)
            {
                return;
            }

            _number = value;
            OnPropertyChanged();
            OnPropertyChanged(nameof(Heading));
        }
    }

    public SkillListItem? SelectedCandidate
    {
        get => _selectedCandidate;
        set
        {
            if (_selectedCandidate == value)
            {
                return;
            }

            _selectedCandidate = value;
            OnPropertyChanged();
            AddMemberCommand.NotifyCanExecuteChanged();
        }
    }

    internal void AddMember(SkillListItem skill)
    {
        Members.Add(new PrerequisiteMemberViewModel(skill, _labels.RemoveMember, RemoveMember));
        AddMemberCommand.NotifyCanExecuteChanged();
        _changed();
    }

    private bool CanAddSelectedCandidate() =>
        SelectedCandidate is { } candidate && Members.All(member => member.Skill.SkillId != candidate.SkillId);

    private void AddSelectedCandidate()
    {
        if (SelectedCandidate is { } candidate)
        {
            AddMember(candidate);
            SelectedCandidate = null;
        }
    }

    private void RemoveMember(PrerequisiteMemberViewModel member)
    {
        Members.Remove(member);
        AddMemberCommand.NotifyCanExecuteChanged();
        _changed();
    }
}
