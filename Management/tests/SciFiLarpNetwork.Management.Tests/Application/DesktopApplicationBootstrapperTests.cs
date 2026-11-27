using SciFiLarpNetwork.Management.Views.News;
using SciFiLarpNetwork.Management.Views.CardTemplates;
using SciFiLarpNetwork.Management.Views.LarpEvents;
using System.Text.Json;
using Avalonia.Controls.ApplicationLifetimes;
using SciFiLarpNetwork.Management.Views.Messages;
using SciFiLarpNetwork.Management.Views.Players;
using SciFiLarpNetwork.Common.Siren;
using SciFiLarpNetwork.Management.Api;
using SciFiLarpNetwork.Management.Application;
using SciFiLarpNetwork.Management.Localization;
using SciFiLarpNetwork.Management.Settings;
using SciFiLarpNetwork.Management.ViewModels;
using SciFiLarpNetwork.Management.Views;
using SciFiLarpNetwork.Management.Views.Networks;
using SciFiLarpNetwork.Management.Views.Skills;

namespace SciFiLarpNetwork.Management.Tests.Application;

public sealed class DesktopApplicationBootstrapperTests
{
    [Fact]
    public Task ConfigureWithDesktopLifetimeCreatesAndAssignsMainWindow() => AvaloniaUi.Run(() =>
    {
        var viewModel = new MainWindowViewModel(
            new TestApiClient(),
            new TestSettingsStore(),
            new JsonLocalizer(),
            new WindowCatalogDialogService(() => throw new InvalidOperationException("No dialog is expected.")),
            new WindowNetworkDialogService(() => throw new InvalidOperationException("No dialog is expected.")),
            new WindowPlayerDialogService(() => throw new InvalidOperationException("No dialog is expected.")),
            new WindowCardTemplateDialogService(() => throw new InvalidOperationException("No dialog is expected.")),
            new WindowLarpEventDialogService(() => throw new InvalidOperationException("No dialog is expected.")),
            new WindowMessageDialogService(() => throw new InvalidOperationException("No dialog is expected.")),
            new WindowNewsDialogService(() => throw new InvalidOperationException("No dialog is expected.")),
            new SciFiLarpNetwork.Management.Views.Items.WindowItemDialogService(() => throw new InvalidOperationException("No dialog is expected.")),
            new ManagementSettings());
        var window = new MainWindow(viewModel);
        var lifetime = new ClassicDesktopStyleApplicationLifetime();
        var bootstrapper = new DesktopApplicationBootstrapper(() => window);

        bootstrapper.Configure(lifetime);

        Assert.Same(window, lifetime.MainWindow);
    });

    [Fact]
    public Task ConfigureWithoutDesktopLifetimeDoesNotCreateMainWindow() => AvaloniaUi.Run(() =>
    {
        var factoryCalled = false;
        var bootstrapper = new DesktopApplicationBootstrapper(() =>
        {
            factoryCalled = true;
            throw new InvalidOperationException("The desktop window must not be created.");
        });
        bootstrapper.Configure(null);
        Assert.False(factoryCalled);
    });

    private sealed class TestApiClient : IManagementApiClient
    {
        public Task<ManagementSession> SignInAsync(
            string apiBaseUrl,
            string userName,
            string password,
            string locale,
            CancellationToken cancellationToken = default) =>
            Task.FromException<ManagementSession>(new NotImplementedException());

        public Task SignOutAsync(
            string apiBaseUrl,
            string locale,
            CancellationToken cancellationToken = default) =>
            Task.FromException(new NotImplementedException());

        public Task<ManagementSession> RenewSessionAsync(
            string apiBaseUrl,
            string locale,
            CancellationToken cancellationToken = default) =>
            Task.FromException<ManagementSession>(new NotImplementedException());

        public Task<ApiDiscovery> DiscoverAsync(
            string apiBaseUrl,
            string locale,
            CancellationToken cancellationToken = default) =>
            Task.FromException<ApiDiscovery>(new NotImplementedException());

        public Task<SirenEntity> GetResourceAsync(
            string apiBaseUrl,
            string href,
            string locale,
            CancellationToken cancellationToken = default) =>
            Task.FromException<SirenEntity<JsonElement>>(new NotImplementedException());

        public Task<byte[]> DownloadActionAsync(
            string apiBaseUrl,
            SirenAction action,
            JsonElement body,
            string mediaType,
            string locale,
            CancellationToken cancellationToken = default) =>
            Task.FromException<byte[]>(new NotSupportedException("This fake prints no files."));

        public Task<byte[]> GetFileAsync(string apiBaseUrl, string href, string mediaType, string locale,
            CancellationToken cancellationToken = default) =>
            Task.FromException<byte[]>(new NotSupportedException("This fake serves no files."));

        public Task<byte[]> GetImageAsync(
            string apiBaseUrl,
            string href,
            string locale,
            CancellationToken cancellationToken = default) =>
            Task.FromException<byte[]>(new NotImplementedException());

        public Task<SirenEntity> SubmitActionAsync(
            string apiBaseUrl,
            SirenAction action,
            JsonElement? body,
            string locale,
            CancellationToken cancellationToken = default) =>
            Task.FromException<SirenEntity<JsonElement>>(new NotImplementedException());
    }

    private sealed class TestSettingsStore : IManagementSettingsStore
    {
        public ManagementSettings Load() => new();

        public void Save(ManagementSettings settings)
        {
        }
    }
}
