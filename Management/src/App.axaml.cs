using System.Text.Json;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Markup.Xaml;
using SciFiLarpNetwork.Management.Api;
using SciFiLarpNetwork.Management.Application;
using SciFiLarpNetwork.Management.Localization;
using SciFiLarpNetwork.Management.Settings;
using SciFiLarpNetwork.Management.ViewModels;
using SciFiLarpNetwork.Management.Views;
using SciFiLarpNetwork.Common.Localization;

namespace SciFiLarpNetwork.Management;

public partial class App : Avalonia.Application
{
    public override void Initialize() =>
        AvaloniaXamlLoader.Load(this);

    public override void OnFrameworkInitializationCompleted()
    {
        if (ApplicationLifetime is IClassicDesktopStyleApplicationLifetime desktop)
        {
            var httpClient = new HttpClient(new HttpClientHandler { AllowAutoRedirect = false });
            desktop.Exit += (_, _) => httpClient.Dispose();

            new DesktopApplicationBootstrapper(() =>
                    CreateMainWindow(ManagementSettingsStore.DefaultSettingsPath, httpClient))
                .Configure(desktop);
        }

        base.OnFrameworkInitializationCompleted();
    }

    internal static MainWindow CreateMainWindow(string settingsPath, HttpClient httpClient)
    {
        var settingsStore = new ManagementSettingsStore(settingsPath);
        ManagementSettings settings;
        string? startupErrorKey = null;
        try
        {
            settings = settingsStore.Load();
        }
        catch (JsonException)
        {
            settings = new ManagementSettings();
            startupErrorKey = "management.settingsLoadError";
        }
        catch (IOException)
        {
            settings = new ManagementSettings();
            startupErrorKey = "management.settingsLoadError";
        }
        catch (UnauthorizedAccessException)
        {
            settings = new ManagementSettings();
            startupErrorKey = "management.settingsLoadError";
        }

        var localizer = new JsonLocalizer(new LocalizationCatalog());
        var apiClient = new ManagementApiClient(httpClient);
        return new MainWindow(
            new MainWindowViewModel(apiClient, settingsStore, localizer, settings, startupErrorKey));
    }
}
