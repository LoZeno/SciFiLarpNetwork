using System.Text.Json;
using SciFiLarpNetwork.Management.Localization;

namespace SciFiLarpNetwork.Management.ViewModels.LarpEvents;

/// <summary>The event dialog: its name, description, and base experience.</summary>
public sealed class LarpEventEditorViewModel : CatalogEditorViewModel
{
    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web);
    private readonly Func<LarpEventEditorViewModel, Task<bool>> _submit;
    private string _baseExperience = string.Empty;

    internal LarpEventEditorViewModel(
        IManagementLocalizer localizer,
        string locale,
        string titleKey,
        Func<LarpEventEditorViewModel, Task<bool>> submit)
        : base(localizer, locale, titleKey)
    {
        _submit = submit;
    }

    public string EventNameLabel => Translate("events.name");

    public string EventDescriptionLabel => Translate("events.description");

    public string BaseExperienceLabel => Translate("events.baseExperience");

    public string ExperienceHelp => Translate("events.experienceValueHelp");

    /// <summary>The typed base experience; blank for none.</summary>
    public string BaseExperience
    {
        get => _baseExperience;
        set
        {
            if (_baseExperience == value)
            {
                return;
            }

            _baseExperience = value;
            OnPropertyChanged();
        }
    }

    internal JsonElement CreateBody(long? version)
    {
        var baseExperience = ExperienceValueText.Parse(BaseExperience);
        var description = Blank(Description);
        return version is { } current
            ? JsonSerializer.SerializeToElement(new { version = current, name = Name, description, baseExperience }, JsonOptions)
            : JsonSerializer.SerializeToElement(new { name = Name, description, baseExperience }, JsonOptions);
    }

    private protected override async Task<bool> SubmitAsync()
    {
        if (!ExperienceValueText.TryParse(BaseExperience, out _))
        {
            ErrorMessage = Translate("events.invalidNumber");
            return false;
        }

        return await _submit(this);
    }

    private static string? Blank(string value) => string.IsNullOrWhiteSpace(value) ? null : value;
}
