using System.Reflection;
using System.Text.Json;

namespace SciFiLarpNetwork.Common.Localization;

public sealed class LocalizationCatalog
{
    private const string ResourcePrefix = "SciFiLarpNetwork.Common.Localization.";
    private readonly Dictionary<string, string> _english;
    private readonly Dictionary<string, string> _italian;

    public LocalizationCatalog()
    {
        var assembly = typeof(LocalizationCatalog).Assembly;
        _english = ReadLanguage(assembly, "en");
        _italian = ReadLanguage(assembly, "it");
    }

    public string GetText(string key, string locale)
    {
        if (TryGetText(key, locale, out var value))
        {
            return value;
        }

        throw new KeyNotFoundException($"The localization key '{key}' does not exist.");
    }

    public bool TryGetText(string key, string locale, out string value)
    {
        var translations = locale.Equals("it", StringComparison.OrdinalIgnoreCase)
            ? _italian
            : _english;

        if (translations.TryGetValue(key, out value!))
        {
            return true;
        }

        return _english.TryGetValue(key, out value!);
    }

    private static Dictionary<string, string> ReadLanguage(Assembly assembly, string language)
    {
        var resourceName = $"{ResourcePrefix}{language}.json";
        using var stream = assembly.GetManifestResourceStream(resourceName)
            ?? throw new InvalidOperationException($"The embedded localization resource '{resourceName}' is missing.");

        var values = JsonSerializer.Deserialize<Dictionary<string, string>>(stream)
            ?? throw new InvalidOperationException($"The embedded localization resource '{resourceName}' is empty.");

        if (values.Any(entry => string.IsNullOrWhiteSpace(entry.Key) || string.IsNullOrWhiteSpace(entry.Value)))
        {
            throw new InvalidOperationException($"The embedded localization resource '{resourceName}' contains an empty key or value.");
        }

        return values;
    }
}
