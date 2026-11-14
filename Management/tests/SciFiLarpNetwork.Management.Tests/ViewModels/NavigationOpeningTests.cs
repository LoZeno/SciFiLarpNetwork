using System.Text.Json;
using SciFiLarpNetwork.Common.Siren;
using SciFiLarpNetwork.Management.Api;
using SciFiLarpNetwork.Management.Localization;
using SciFiLarpNetwork.Management.Settings;
using SciFiLarpNetwork.Management.Tests.ViewModels.Skills;
using SciFiLarpNetwork.Management.ViewModels;

namespace SciFiLarpNetwork.Management.Tests.ViewModels;

public sealed class NavigationOpeningTests
{
    private static readonly NavigationItem News = new("News", "/api/v1/news");
    private static readonly NavigationItem Messages = new("Messages", "/api/v1/messages");

    [Fact]
    public void SelectingAnotherEntryWhileOneIsOpeningShowsTheNewerEntry()
    {
        var api = new ScriptedApiClient();
        var heldNews = api.Hold(News.Href);
        var viewModel = CreateViewModel(api);
        viewModel.SelectedNavigationItem = viewModel.NavigationItems[0];
        viewModel.SelectedNavigationItem = viewModel.NavigationItems[1];

        heldNews.SetResult(Titled("News"));

        Assert.Equal("Messages", viewModel.ResourceTitle);
    }

    [Fact]
    public void AFailedOpenClearsTheSelection()
    {
        var api = new ScriptedApiClient();
        api.FailNext(News.Href, new HttpRequestException());
        var viewModel = CreateViewModel(api);

        viewModel.SelectedNavigationItem = viewModel.NavigationItems[0];

        Assert.Null(viewModel.SelectedNavigationItem);
    }

    [Fact]
    public void SelectingAFailedEntryAgainRetriesIt()
    {
        var api = new ScriptedApiClient();
        api.FailNext(News.Href, new HttpRequestException());
        var viewModel = CreateViewModel(api);
        viewModel.SelectedNavigationItem = viewModel.NavigationItems[0];

        viewModel.SelectedNavigationItem = viewModel.NavigationItems[0];

        Assert.Equal("News", viewModel.ResourceTitle);
    }

    [Fact]
    public void AnOlderOpenThatFailsLateDoesNotReportItsError()
    {
        var api = new ScriptedApiClient();
        var heldNews = api.Hold(News.Href);
        var viewModel = CreateViewModel(api);
        viewModel.SelectedNavigationItem = viewModel.NavigationItems[0];
        viewModel.SelectedNavigationItem = viewModel.NavigationItems[1];

        heldNews.SetException(new HttpRequestException());

        Assert.Equal("API connection established", viewModel.StatusMessage);
    }

    private static MainWindowViewModel CreateViewModel(ScriptedApiClient api)
    {
        var viewModel = new MainWindowViewModel(
            api,
            new NullSettingsStore(),
            new JsonLocalizer(),
            new FakeCatalogDialogs(),
            new ManagementSettings());
        viewModel.NavigationItems.Add(News);
        viewModel.NavigationItems.Add(Messages);
        return viewModel;
    }

    private static SirenEntity Titled(string title) =>
        new() { Class = ["resource"], Properties = JsonSerializer.SerializeToElement(new { title }) };

    private sealed class ScriptedApiClient : IManagementApiClient
    {
        private readonly Dictionary<string, TaskCompletionSource<SirenEntity>> _held = [];
        private readonly Dictionary<string, Exception> _failures = [];

        public TaskCompletionSource<SirenEntity> Hold(string href)
        {
            var completion = new TaskCompletionSource<SirenEntity>();
            _held[href] = completion;
            return completion;
        }

        public void FailNext(string href, Exception failure) => _failures[href] = failure;

        public Task<SirenEntity> GetResourceAsync(
            string apiBaseUrl,
            string href,
            string locale,
            CancellationToken cancellationToken = default)
        {
            if (_held.Remove(href, out var held))
            {
                return held.Task;
            }

            return _failures.Remove(href, out var failure)
                ? Task.FromException<SirenEntity>(failure)
                : Task.FromResult(Titled(href == News.Href ? "News" : "Messages"));
        }

        public Task<ManagementSession> SignInAsync(
            string apiBaseUrl,
            string userName,
            string password,
            string locale,
            CancellationToken cancellationToken = default) =>
            Task.FromResult(new ManagementSession(userName, "administrator", DateTimeOffset.UtcNow.AddHours(1)));

        public Task SignOutAsync(string apiBaseUrl, string locale, CancellationToken cancellationToken = default) =>
            Task.CompletedTask;

        public Task<ManagementSession> RenewSessionAsync(
            string apiBaseUrl,
            string locale,
            CancellationToken cancellationToken = default) =>
            Task.FromResult(new ManagementSession("operator", "administrator", DateTimeOffset.UtcNow.AddHours(1)));

        public Task<ApiDiscovery> DiscoverAsync(string apiBaseUrl, string locale, CancellationToken cancellationToken = default) =>
            Task.FromResult(new ApiDiscovery(Titled("Administration"), []));

        public Task<SirenEntity> SubmitActionAsync(
            string apiBaseUrl,
            SirenAction action,
            JsonElement? body,
            string locale,
            CancellationToken cancellationToken = default) =>
            Task.FromResult(Titled("Done"));
    }

    private sealed class NullSettingsStore : IManagementSettingsStore
    {
        public ManagementSettings Load() => new();

        public void Save(ManagementSettings settings)
        {
        }
    }
}
