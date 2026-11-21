using System.Text.Json;
using SciFiLarpNetwork.Management.Localization;

namespace SciFiLarpNetwork.Management.ViewModels.LarpEvents;

/// <summary>The day dialog: the date of a new day, or the new date of an existing one.</summary>
public sealed class DayEditorViewModel : CatalogEditorViewModel
{
    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web);
    private readonly Func<DayEditorViewModel, Task<bool>> _submit;
    private DateTime? _date;

    internal DayEditorViewModel(
        IManagementLocalizer localizer,
        string locale,
        string titleKey,
        DateOnly? date,
        Func<DayEditorViewModel, Task<bool>> submit)
        : base(localizer, locale, titleKey)
    {
        _date = date?.ToDateTime(TimeOnly.MinValue);
        _submit = submit;
    }

    public string DateLabel => Translate("events.date");

    /// <summary>The chosen date, as the date picker binds it.</summary>
    public DateTime? Date
    {
        get => _date;
        set
        {
            if (_date == value)
            {
                return;
            }

            _date = value;
            OnPropertyChanged();
        }
    }

    internal bool HasDate => Date is not null;

    /// <param name="dayId">The day to reschedule; <see langword="null"/> when adding a day.</param>
    internal JsonElement CreateBody(Guid? dayId)
    {
        var date = DateOnly.FromDateTime(Date ?? throw new InvalidOperationException("No date was chosen."));
        return dayId is { } existing
            ? JsonSerializer.SerializeToElement(new { dayId = existing, date }, JsonOptions)
            : JsonSerializer.SerializeToElement(new { date }, JsonOptions);
    }

    private protected override async Task<bool> SubmitAsync()
    {
        if (!HasDate)
        {
            ErrorMessage = Translate("events.dateRequired");
            return false;
        }

        return await _submit(this);
    }
}
