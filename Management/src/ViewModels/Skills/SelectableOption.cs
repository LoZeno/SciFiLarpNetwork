namespace SciFiLarpNetwork.Management.ViewModels.Skills;

/// <param name="value">The value sent to the API when the option is selected.</param>
public sealed class SelectableOption(string value, string title, bool isSelected = false) : ObservableViewModel
{
    private bool _isSelected = isSelected;

    public string Value { get; } = value;

    public string Title { get; } = title;

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
}
