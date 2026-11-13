using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Headless.XUnit;
using Avalonia.Threading;
using SciFiLarpNetwork.Management.Settings;
using SciFiLarpNetwork.Management.ViewModels;
using SciFiLarpNetwork.Management.Views;

namespace SciFiLarpNetwork.Management.Tests.Application;

public sealed class AppStartupTests
{
    [AvaloniaFact]
    public void CreateMainWindowLoadsPersistedSettings()
    {
        using var directory = new TemporaryDirectory();
        using var httpClient = new HttpClient();
        var settingsPath = Path.Combine(directory.Path, "settings.json");
        new ManagementSettingsStore(settingsPath)
            .Save(new ManagementSettings("http://localhost:6000/api", "it"));

        var window = App.CreateMainWindow(settingsPath, httpClient);
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
    }

    [AvaloniaFact]
    public void CreateMainWindowReportsMalformedSettingsAndUsesDefaults()
    {
        using var directory = new TemporaryDirectory();
        using var httpClient = new HttpClient();
        var settingsPath = Path.Combine(directory.Path, "settings.json");
        File.WriteAllText(settingsPath, "{");

        var window = App.CreateMainWindow(settingsPath, httpClient);
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
    }

    [AvaloniaFact]
    public void CreateMainWindowReportsUnreadableSettingsAndUsesDefaults()
    {
        using var directory = new TemporaryDirectory();
        using var httpClient = new HttpClient();
        var settingsPath = Path.Combine(directory.Path, "settings.json");
        File.WriteAllText(settingsPath, "{}");
        using var exclusiveLock = new FileStream(settingsPath, FileMode.Open, FileAccess.ReadWrite, FileShare.None);

        var window = App.CreateMainWindow(settingsPath, httpClient);
        var statusMessage = ((MainWindowViewModel)window.DataContext!).StatusMessage;
        window.Close();

        Assert.Equal("Management settings could not be loaded. Defaults are being used.", statusMessage);
    }

    [AvaloniaFact]
    public void FrameworkInitializationAssignsMainWindowToDesktopLifetime()
    {
        using var lifetime = new ClassicDesktopStyleApplicationLifetime();
        var app = new App { ApplicationLifetime = lifetime };

        app.OnFrameworkInitializationCompleted();

        Assert.IsType<MainWindow>(lifetime.MainWindow);
        lifetime.MainWindow.Close();
    }

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

        public void Dispose()
        {
            Directory.Delete(Path, recursive: true);
            GC.SuppressFinalize(this);
        }
    }
}
