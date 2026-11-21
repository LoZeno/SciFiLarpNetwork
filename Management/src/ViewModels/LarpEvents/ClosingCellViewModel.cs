namespace SciFiLarpNetwork.Management.ViewModels.LarpEvents;

/// <summary>
/// One cell of the closing grid: a checkbox for experience that can still be given, or a short text for experience
/// already given, a day without experience, or a day the character is not registered for.
/// </summary>
public sealed class ClosingCellViewModel : ObservableViewModel
{
    private bool _isChecked;

    private ClosingCellViewModel(Guid? dayId, bool isCheckbox, bool isChecked, string text)
    {
        DayId = dayId;
        IsCheckbox = isCheckbox;
        _isChecked = isChecked;
        Text = text;
    }

    /// <summary>The day of a day cell; <see langword="null"/> for the base-experience cell.</summary>
    public Guid? DayId { get; }

    public bool IsCheckbox { get; }

    public bool IsText => !IsCheckbox;

    public string Text { get; }

    public bool IsChecked
    {
        get => _isChecked;
        set
        {
            if (_isChecked == value || !IsCheckbox)
            {
                return;
            }

            _isChecked = value;
            OnPropertyChanged();
        }
    }

    public static ClosingCellViewModel Checkbox(Guid? dayId, bool isChecked) => new(dayId, true, isChecked, string.Empty);

    public static ClosingCellViewModel Label(Guid? dayId, string text) => new(dayId, false, false, text);
}
