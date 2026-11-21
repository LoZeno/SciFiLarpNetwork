using System.Text.Json;
using SciFiLarpNetwork.Management.Localization;

namespace SciFiLarpNetwork.Management.ViewModels.LarpEvents;

/// <summary>The event dialog: its name and description.</summary>
public sealed class LarpEventEditorViewModel : CatalogEditorViewModel
{
    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web);
    private readonly Func<LarpEventEditorViewModel, Task<bool>> _submit;

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

    internal JsonElement CreateBody(long? version) =>
        version is { } current
            ? JsonSerializer.SerializeToElement(new { version = current, name = Name, description = Blank(Description) }, JsonOptions)
            : JsonSerializer.SerializeToElement(new { name = Name, description = Blank(Description) }, JsonOptions);

    private protected override Task<bool> SubmitAsync() => _submit(this);

    private static string? Blank(string value) => string.IsNullOrWhiteSpace(value) ? null : value;
}
