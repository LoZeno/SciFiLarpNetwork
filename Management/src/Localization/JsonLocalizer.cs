using SciFiLarpNetwork.Common.Localization;

namespace SciFiLarpNetwork.Management.Localization;

public sealed class JsonLocalizer(LocalizationCatalog catalog) : IManagementLocalizer
{
    public JsonLocalizer() : this(new LocalizationCatalog())
    {
    }

    public string Translate(string key, string locale)
    {
        return catalog.TryGetText(key, locale, out var value)
            ? value
            : catalog.GetText("common.unknownError", "en");
    }
}
