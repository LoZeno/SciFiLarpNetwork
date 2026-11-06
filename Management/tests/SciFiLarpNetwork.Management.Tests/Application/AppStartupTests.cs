using SciFiLarpNetwork.Management.Settings;
using SciFiLarpNetwork.Management.ViewModels;
using SciFiLarpNetwork.Management.Views;
using Avalonia.Threading;

namespace SciFiLarpNetwork.Management.Tests.Application;

[Collection(AvaloniaHeadlessTestGroup.Name)]
public sealed class AppStartupTests(AvaloniaHeadlessFixture avalonia)
{
    [Fact]
    public Task CreateMainWindowLoadsPersistedSettings() =>
        avalonia.Session.Dispatch(() =>
        {
            using var directory = new TemporaryDirectory();
            using var httpClient = new HttpClient();
            var settingsPath = Path.Combine(directory.Path, "settings.json");
            new ManagementSettingsStore(settingsPath)
                .Save(new ManagementSettings("http://localhost:6000/api", "it"));

            var window = SciFiLarpNetwork.Management.App.CreateMainWindow(settingsPath, httpClient);
            var viewModel = (MainWindowViewModel)window.DataContext!;

            window.Show();
            Dispatcher.UIThread.RunJobs();
            var actual = new MainWindowStartupSnapshot(
                window.GetType(),
                window.Title ?? string.Empty,
                new ManagementSettings(viewModel.ApiBaseUrl, viewModel.Locale),
                viewModel.StatusMessage);
            window.Close();

            Assert.Equal(
                new MainWindowStartupSnapshot(
                    typeof(MainWindow),
                    "Sci-Fi LARP Network",
                    new ManagementSettings("http://localhost:6000/api", "it"),
                    "Al momento non sono disponibili destinazioni."),
                actual);
        }, CancellationToken.None);

    [Fact]
    public Task CreateMainWindowReportsMalformedSettingsAndUsesDefaults() =>
        avalonia.Session.Dispatch(() =>
        {
            using var directory = new TemporaryDirectory();
            using var httpClient = new HttpClient();
            var settingsPath = Path.Combine(directory.Path, "settings.json");
            File.WriteAllText(settingsPath, "{");

            var window = SciFiLarpNetwork.Management.App.CreateMainWindow(settingsPath, httpClient);
            var viewModel = (MainWindowViewModel)window.DataContext!;
            var actual = new MainWindowStartupSnapshot(
                window.GetType(),
                window.Title ?? string.Empty,
                new ManagementSettings(viewModel.ApiBaseUrl, viewModel.Locale),
                viewModel.StatusMessage);
            window.Close();

            Assert.Equal(
                new MainWindowStartupSnapshot(
                    typeof(MainWindow),
                    "Sci-Fi LARP Network",
                    new ManagementSettings(),
                    "Management settings could not be loaded. Defaults are being used."),
                actual);
        }, CancellationToken.None);

    private sealed record MainWindowStartupSnapshot(
        Type WindowType,
        string Title,
        ManagementSettings Settings,
        string StatusMessage);

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
