using System.Text.Json;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Markup.Xaml;
using SciFiLarpNetwork.Common.Localization;
using SciFiLarpNetwork.Management.Api;
using SciFiLarpNetwork.Management.Application;
using SciFiLarpNetwork.Management.Localization;
using SciFiLarpNetwork.Management.Settings;
using SciFiLarpNetwork.Management.ViewModels;
using SciFiLarpNetwork.Management.Views;
using SciFiLarpNetwork.Management.Views.CardTemplates;
using SciFiLarpNetwork.Management.Views.LarpEvents;
using SciFiLarpNetwork.Management.Views.Messages;
using SciFiLarpNetwork.Management.Views.News;
using SciFiLarpNetwork.Management.Views.Networks;
using SciFiLarpNetwork.Management.Views.Players;
using SciFiLarpNetwork.Management.Views.Skills;

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

    public static MainWindow CreateMainWindow(string settingsPath, HttpClient httpClient)
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
        MainWindow? window = null;
        var catalogDialogs = new WindowCatalogDialogService(() => window!);
        var networkDialogs = new WindowNetworkDialogService(() => window!);
        var playerDialogs = new WindowPlayerDialogService(() => window!);
        var cardTemplateDialogs = new WindowCardTemplateDialogService(() => window!);
        var eventDialogs = new WindowLarpEventDialogService(() => window!);
        var messageDialogs = new WindowMessageDialogService(() => window!);
        var newsDialogs = new WindowNewsDialogService(() => window!);
        window = new MainWindow(
            new MainWindowViewModel(
                apiClient, settingsStore, localizer, catalogDialogs, networkDialogs, playerDialogs, cardTemplateDialogs,
                eventDialogs, messageDialogs, newsDialogs, settings, startupErrorKey));
        return window;
    }
}
