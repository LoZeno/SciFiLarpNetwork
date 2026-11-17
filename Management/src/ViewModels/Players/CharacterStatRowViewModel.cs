namespace SciFiLarpNetwork.Management.ViewModels.Players;

/// <summary>One existing stat in the character dialog, with the value typed for this character.</summary>
public sealed class CharacterStatRowViewModel(Guid statId, string name, bool isNumber, bool isArchived, string value)
    : ObservableViewModel
{
    private string _value = value;

    public Guid StatId { get; } = statId;

    public string Name { get; } = name;

    public bool IsNumber { get; } = isNumber;

    public bool IsArchived { get; } = isArchived;

    public string Value
    {
        get => _value;
        set
        {
            if (_value == value)
            {
                return;
            }

            _value = value;
            OnPropertyChanged();
        }
    }
}
