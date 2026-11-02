using System.Globalization;
using System.Reflection;
using System.Text.Json;

namespace SciFiLarpNetwork.Backend.Localization.Text;

public sealed class ApplicationText : IApplicationText
{
    private const string ResourcePrefix = "SciFiLarpNetwork.Backend.Localization.";
    private readonly Dictionary<string, string> _english;
    private readonly Dictionary<string, string> _italian;

    public ApplicationText()
    {
        var assembly = typeof(ApplicationText).Assembly;
        _english = ReadLanguage(assembly, "en");
        _italian = ReadLanguage(assembly, "it");
    }

    public string GetLocalizedText(string key)
    {
        var translations = CultureInfo.CurrentUICulture.TwoLetterISOLanguageName == "it"
            ? _italian
            : _english;

        if (translations.TryGetValue(key, out var value))
        {
            return value;
        }

        if (_english.TryGetValue(key, out value))
        {
            return value;
        }

        throw new KeyNotFoundException($"The localization key '{key}' does not exist.");
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
