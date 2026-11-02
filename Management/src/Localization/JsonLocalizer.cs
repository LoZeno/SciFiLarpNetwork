using System.Reflection;
using System.Text.Json;

namespace SciFiLarpNetwork.Management.Localization;

public sealed class JsonLocalizer : IManagementLocalizer
{
    private const string ResourcePrefix = "SciFiLarpNetwork.Management.Localization.";
    private readonly Dictionary<string, string> _english = Load("en");
    private readonly Dictionary<string, string> _italian = Load("it");

    public string Translate(string key, string locale)
    {
        var resources = locale.Equals("it", StringComparison.OrdinalIgnoreCase)
            ? _italian
            : _english;

        if (resources.TryGetValue(key, out var value))
        {
            return value;
        }

        if (_english.TryGetValue(key, out value))
        {
            return value;
        }

        return _english["common.unknownError"];
    }

    private static Dictionary<string, string> Load(string locale)
    {
        var resourceName = $"{ResourcePrefix}{locale}.json";
        using var stream = Assembly.GetExecutingAssembly().GetManifestResourceStream(resourceName)
            ?? throw new InvalidOperationException($"Missing embedded localization resource '{resourceName}'.");
        return JsonSerializer.Deserialize<Dictionary<string, string>>(stream)
            ?? throw new JsonException($"Localization resource '{resourceName}' is empty.");
    }
}
