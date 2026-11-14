using System.Text.Json;
using System.Text.Json.Serialization;
using SciFiLarpNetwork.Management.Localization;

namespace SciFiLarpNetwork.Management.ViewModels.Skills;

public sealed class TalentEditorViewModel(
    IManagementLocalizer localizer,
    string locale,
    string titleKey,
    Func<TalentEditorViewModel, Task<bool>> submit)
    : CatalogEditorViewModel(localizer, locale, titleKey)
{
    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web)
    {
        DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull
    };

    internal JsonElement CreateRequestBody(long? version) =>
        JsonSerializer.SerializeToElement(
            new
            {
                version,
                name = Name,
                description = string.IsNullOrWhiteSpace(Description) ? null : Description
            },
            JsonOptions);

    private protected override Task<bool> SubmitAsync() => submit(this);
}
