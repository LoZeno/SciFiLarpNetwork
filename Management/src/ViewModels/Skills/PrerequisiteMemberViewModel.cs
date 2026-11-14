namespace SciFiLarpNetwork.Management.ViewModels.Skills;

public sealed class PrerequisiteMemberViewModel
{
    internal PrerequisiteMemberViewModel(SkillListItem skill, string removeLabel, Action<PrerequisiteMemberViewModel> remove)
    {
        Skill = skill;
        RemoveLabel = removeLabel;
        RemoveCommand = new DelegateCommand(() => remove(this));
    }

    public SkillListItem Skill { get; }

    public string RemoveLabel { get; }

    public DelegateCommand RemoveCommand { get; }
}
