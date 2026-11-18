namespace SciFiLarpNetwork.Management.ViewModels.Players;

/// <summary>One of the character's talents. It can be removed only while no skill is bought through it.</summary>
public sealed class CharacterTalentRowViewModel
{
    internal CharacterTalentRowViewModel(Guid talentId, string title, Action<CharacterTalentRowViewModel> remove,
        Func<CharacterTalentRowViewModel, bool> canRemove)
    {
        TalentId = talentId;
        Title = title;
        RemoveCommand = new DelegateCommand(() => remove(this), () => canRemove(this));
    }

    public Guid TalentId { get; }

    public string Title { get; }

    public DelegateCommand RemoveCommand { get; }
}
