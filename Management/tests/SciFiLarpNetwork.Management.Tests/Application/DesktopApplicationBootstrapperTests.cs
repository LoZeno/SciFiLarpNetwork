using System.Text.Json;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Headless.XUnit;
using SciFiLarpNetwork.Management.Api;
using SciFiLarpNetwork.Management.Application;
using SciFiLarpNetwork.Management.Localization;
using SciFiLarpNetwork.Management.Settings;
using SciFiLarpNetwork.Management.ViewModels;
using SciFiLarpNetwork.Management.Views;
using SciFiLarpNetwork.Common.Siren;

namespace SciFiLarpNetwork.Management.Tests.Application;

public sealed class DesktopApplicationBootstrapperTests
{
    [AvaloniaFact]
    public void ConfigureWithDesktopLifetimeCreatesAndAssignsMainWindow()
    {
        var viewModel = new MainWindowViewModel(
            new TestApiClient(),
            new TestSettingsStore(),
            new JsonLocalizer(),
            new ManagementSettings());
        var window = new MainWindow(viewModel);
        var lifetime = new ClassicDesktopStyleApplicationLifetime();
        var bootstrapper = new DesktopApplicationBootstrapper(() => window);

        bootstrapper.Configure(lifetime);

        Assert.Same(window, lifetime.MainWindow);
    }

    [AvaloniaFact]
    public void ConfigureWithoutDesktopLifetimeDoesNotCreateMainWindow()
    {
        var factoryCalled = false;
        var bootstrapper = new DesktopApplicationBootstrapper(() =>
        {
            factoryCalled = true;
            throw new InvalidOperationException("The desktop window must not be created.");
        });
        bootstrapper.Configure(null);
        Assert.False(factoryCalled);
    }

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

        public Task<SirenEntity<JsonElement>> GetResourceAsync(
            string apiBaseUrl,
            string href,
            string locale,
            CancellationToken cancellationToken = default) =>
            Task.FromException<SirenEntity<JsonElement>>(new NotImplementedException());

        public Task<SirenEntity<JsonElement>> SubmitActionAsync(
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
