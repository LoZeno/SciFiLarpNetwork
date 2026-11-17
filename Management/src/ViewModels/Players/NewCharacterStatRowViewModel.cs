namespace SciFiLarpNetwork.Management.ViewModels.Players;

/// <summary>A stat typed in the character dialog. Saving defines it for every character when the name is new.</summary>
public sealed class NewCharacterStatRowViewModel : ObservableViewModel
{
    private string _name = string.Empty;
    private string _value = string.Empty;
    private RosterChoice? _selectedType;

    internal NewCharacterStatRowViewModel(IReadOnlyList<RosterChoice> types, Action<NewCharacterStatRowViewModel> remove)
    {
        Types = types;
        _selectedType = types.Count > 0 ? types[0] : null;
        RemoveCommand = new DelegateCommand(() => remove(this));
    }

    public IReadOnlyList<RosterChoice> Types { get; }

    public DelegateCommand RemoveCommand { get; }

    public string Name
    {
        get => _name;
        set
        {
            if (_name == value)
            {
                return;
            }

            _name = value;
            OnPropertyChanged();
        }
    }

    public RosterChoice? SelectedType
    {
        get => _selectedType;
        set
        {
            if (_selectedType == value)
            {
                return;
            }

            _selectedType = value;
            OnPropertyChanged();
        }
    }

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
