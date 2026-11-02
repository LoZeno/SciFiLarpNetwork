using SciFiLarpNetwork.Management.Settings;
using SciFiLarpNetwork.Management.ViewModels;
using SciFiLarpNetwork.Management.Views;
using Avalonia.Threading;

namespace SciFiLarpNetwork.Management.Tests.Application;

[Collection(AvaloniaHeadlessTestGroup.Name)]
public sealed class AppStartupTests(AvaloniaHeadlessFixture avalonia)
{
    [Fact]
    public Task CreateMainWindowLoadsPersistedSettings()
    {
        using var directory = new TemporaryDirectory();
        using var httpClient = new HttpClient();

        return avalonia.Session.Dispatch(() =>
        {
            var settingsPath = Path.Combine(directory.Path, "settings.json");
            new ManagementSettingsStore(settingsPath)
                .Save(new ManagementSettings("http://localhost:6000/api", "it"));

            var window = SciFiLarpNetwork.Management.App.CreateMainWindow(settingsPath, httpClient);
            var viewModel = Assert.IsType<MainWindowViewModel>(window.DataContext);

            Assert.IsType<MainWindow>(window);
            window.Show();
            Dispatcher.UIThread.RunJobs();
            Assert.Equal("Sci-Fi LARP Network", window.Title);
            Assert.Equal("http://localhost:6000/api", viewModel.ApiBaseUrl);
            Assert.Equal("it", viewModel.Locale);
            window.Close();
        }, CancellationToken.None);
    }

    [Fact]
    public Task CreateMainWindowReportsMalformedSettingsAndUsesDefaults() =>
        avalonia.Session.Dispatch(() =>
        {
            using var directory = new TemporaryDirectory();
            using var httpClient = new HttpClient();
            var settingsPath = Path.Combine(directory.Path, "settings.json");
            File.WriteAllText(settingsPath, "{");

            var window = SciFiLarpNetwork.Management.App.CreateMainWindow(settingsPath, httpClient);
            var viewModel = Assert.IsType<MainWindowViewModel>(window.DataContext);

            Assert.Equal(new ManagementSettings().ApiBaseUrl, viewModel.ApiBaseUrl);
            Assert.Equal(
                "Management settings could not be loaded. Defaults are being used.",
                viewModel.StatusMessage);
        }, CancellationToken.None);

    private sealed class TemporaryDirectory : IDisposable
    {
        public TemporaryDirectory()
        {
            Path = System.IO.Path.Combine(
                System.IO.Path.GetTempPath(),
                $"scifi-management-startup-{Guid.NewGuid():N}");
            Directory.CreateDirectory(Path);
        }

        public string Path { get; }

        public void Dispose() => Directory.Delete(Path, recursive: true);
    }
}
