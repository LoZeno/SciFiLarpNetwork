using System.Text.Json;

namespace SciFiLarpNetwork.Management.Settings;

public sealed class ManagementSettingsStore(string filePath) : IManagementSettingsStore
{
    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web)
    {
        WriteIndented = true
    };

    public static string DefaultSettingsPath =>
        Path.Combine(AppContext.BaseDirectory, "appsettings.json");

    public ManagementSettings Load()
    {
        if (!File.Exists(filePath))
        {
            return new ManagementSettings();
        }

        using var stream = File.OpenRead(filePath);
        return JsonSerializer.Deserialize<ManagementSettings>(stream, JsonOptions)
            ?? throw new JsonException("The management settings file is empty.");
    }

    public void Save(ManagementSettings settings)
    {
        var directory = Path.GetDirectoryName(filePath)
            ?? throw new InvalidOperationException("The settings file path must include a directory.");
        Directory.CreateDirectory(directory);

        using var stream = File.Create(filePath);
        JsonSerializer.Serialize(stream, settings, JsonOptions);
    }
}
