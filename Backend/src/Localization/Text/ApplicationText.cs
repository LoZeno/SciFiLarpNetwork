using System.Globalization;
using SciFiLarpNetwork.Common.Localization;

namespace SciFiLarpNetwork.Backend.Localization.Text;

public sealed class ApplicationText : IApplicationText
{
    private readonly LocalizationCatalog _catalog = new();

    public string GetLocalizedText(string key) =>
        _catalog.GetText(key, CultureInfo.CurrentUICulture.TwoLetterISOLanguageName);
}
