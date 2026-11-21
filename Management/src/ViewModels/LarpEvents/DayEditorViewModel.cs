using System.Text.Json;
using SciFiLarpNetwork.Management.Localization;

namespace SciFiLarpNetwork.Management.ViewModels.LarpEvents;

/// <summary>The day dialog: the date and experience of a new day, or the new values of an existing one.</summary>
public sealed class DayEditorViewModel : CatalogEditorViewModel
{
    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web);
    private readonly Func<DayEditorViewModel, Task<bool>> _submit;
    private DateTime? _date;
    private string _experience;

    internal DayEditorViewModel(
        IManagementLocalizer localizer,
        string locale,
        string titleKey,
        DateOnly? date,
        int? experience,
        Func<DayEditorViewModel, Task<bool>> submit)
        : base(localizer, locale, titleKey)
    {
        _date = date?.ToDateTime(TimeOnly.MinValue);
        _experience = ExperienceValueText.Format(experience);
        _submit = submit;
    }

    public string DateLabel => Translate("events.date");

    public string ExperienceLabel => Translate("events.dayExperience");

    public string ExperienceHelp => Translate("events.experienceValueHelp");

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

    /// <summary>The typed experience for the day; blank for none.</summary>
    public string Experience
    {
        get => _experience;
        set
        {
            if (_experience == value)
            {
                return;
            }

            _experience = value;
            OnPropertyChanged();
        }
    }

    internal bool HasDate => Date is not null;

    /// <param name="dayId">The day to change; <see langword="null"/> when adding a day.</param>
    internal JsonElement CreateBody(Guid? dayId)
    {
        var date = DateOnly.FromDateTime(Date ?? throw new InvalidOperationException("No date was chosen."));
        var experience = ExperienceValueText.Parse(Experience);
        return dayId is { } existing
            ? JsonSerializer.SerializeToElement(new { dayId = existing, date, experience }, JsonOptions)
            : JsonSerializer.SerializeToElement(new { date, experience }, JsonOptions);
    }

    private protected override async Task<bool> SubmitAsync()
    {
        if (!HasDate)
        {
            ErrorMessage = Translate("events.dateRequired");
            return false;
        }

        if (!ExperienceValueText.TryParse(Experience, out _))
        {
            ErrorMessage = Translate("events.invalidNumber");
            return false;
        }

        return await _submit(this);
    }
}
