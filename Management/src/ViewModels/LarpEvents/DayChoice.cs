namespace SciFiLarpNetwork.Management.ViewModels.LarpEvents;

/// <summary>A day offered in the registration dialog.</summary>
public sealed class DayChoice(Guid dayId, string label) : ObservableViewModel
{
    private bool _isSelected;
    private bool _isRegistered;

    public Guid DayId { get; } = dayId;

    public string Label { get; } = label;

    public bool IsSelected
    {
        get => _isSelected;
        set
        {
            if (_isSelected == value)
            {
                return;
            }

            _isSelected = value;
            OnPropertyChanged();
        }
    }

    /// <summary>Whether the chosen character is already registered for this day; such days cannot be picked.</summary>
    public bool IsRegistered
    {
        get => _isRegistered;
        set
        {
            if (_isRegistered == value)
            {
                return;
            }

            _isRegistered = value;
            OnPropertyChanged();
            OnPropertyChanged(nameof(CanSelect));
        }
    }

    public bool CanSelect => !IsRegistered;
}
