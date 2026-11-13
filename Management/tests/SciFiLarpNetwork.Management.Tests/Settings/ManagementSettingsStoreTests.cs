using System.Text.Json;
using SciFiLarpNetwork.Management.Settings;

namespace SciFiLarpNetwork.Management.Tests.Settings;

public sealed class ManagementSettingsStoreTests
{
    [Fact]
    public void LoadWhenFileDoesNotExistReturnsDefaults()
    {
        using var directory = new TemporaryDirectory();
        var store = new ManagementSettingsStore(Path.Combine(directory.Path, "settings.json"));

        Assert.Equal(new ManagementSettings(), store.Load());
    }

    [Fact]
    public void DefaultSettingsPathIsAnAbsolutePath()
    {
        Assert.Equal(
            Path.Combine(AppContext.BaseDirectory, "appsettings.json"),
            ManagementSettingsStore.DefaultSettingsPath);
    }

    [Fact]
    public void LoadReadsDefaultsFromAppSettingsFileBesideExecutable()
    {
        var store = new ManagementSettingsStore(ManagementSettingsStore.DefaultSettingsPath);

        Assert.Equal(new ManagementSettings(), store.Load());
    }

    [Fact]
    public void SaveAndLoadPersistApiAddressAndLanguage()
    {
        using var directory = new TemporaryDirectory();
        var store = new ManagementSettingsStore(Path.Combine(directory.Path, "settings.json"));
        var settings = new ManagementSettings("http://localhost:6000/api", "it");

        store.Save(settings);

        Assert.Equal(settings, store.Load());
    }

    [Theory]
    [InlineData("http://user:secret@localhost:5000/api")]
    [InlineData("http://localhost:5000/api?token=secret")]
    public void SaveRejectsUrlsContainingCredentials(string apiBaseUrl)
    {
        using var directory = new TemporaryDirectory();
        var store = new ManagementSettingsStore(Path.Combine(directory.Path, "settings.json"));

        Assert.Throws<UriFormatException>(() => store.Save(new ManagementSettings(apiBaseUrl, "en")));
    }

    [Theory]
    [InlineData("http://user:secret@localhost:5000/api")]
    [InlineData("http://localhost:5000/api?token=secret")]
    public void SaveWithCredentialBearingUrlDoesNotReplaceExistingSettings(string apiBaseUrl)
    {
        using var directory = new TemporaryDirectory();
        var store = new ManagementSettingsStore(Path.Combine(directory.Path, "settings.json"));
        var original = new ManagementSettings();
        store.Save(original);

        try
        {
            store.Save(new ManagementSettings(apiBaseUrl, "en"));
        }
        catch (UriFormatException)
        {
        }

        Assert.Equal(original, store.Load());
    }

    [Fact]
    public void LoadWhenFileIsMalformedThrowsJsonException()
    {
        using var directory = new TemporaryDirectory();
        var filePath = Path.Combine(directory.Path, "settings.json");
        File.WriteAllText(filePath, "{");

        Assert.Throws<JsonException>(() => new ManagementSettingsStore(filePath).Load());
    }

    private sealed class TemporaryDirectory : IDisposable
    {
        public TemporaryDirectory()
        {
            Path = System.IO.Path.Combine(
                System.IO.Path.GetTempPath(),
                $"scifi-management-tests-{Guid.NewGuid():N}");
            Directory.CreateDirectory(Path);
        }

        public string Path { get; }

        public void Dispose()
        {
            Directory.Delete(Path, recursive: true);
            GC.SuppressFinalize(this);
        }
    }
}
