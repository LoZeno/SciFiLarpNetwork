using System.Net;
using System.Net.Http;
using System.Text.Json;
using SciFiLarpNetwork.Management.Api;
using SciFiLarpNetwork.Management.Api.Siren;
using SciFiLarpNetwork.Management.Localization;
using SciFiLarpNetwork.Management.Settings;
using SciFiLarpNetwork.Management.ViewModels;

namespace SciFiLarpNetwork.Management.Tests.ViewModels;

public sealed class MainWindowViewModelTests
{
    [Fact]
    public async Task ConnectAsyncLoadsNavigationAndDisplaysAdvertisedActions()
    {
        var apiClient = new StubApiClient
        {
            Discovery = new ApiDiscovery(
                Resource(
                    """{"title":"Administration","description":"Choose a management area."}""",
                    [new SirenAction("inspect", "/api/v1", "GET", null, "Inspect", [])]),
                [
                    new SirenLink(
                        ["urn:scifilarnetwork:rels:navigation"],
                        "/api/v1/messages",
                        "Messages")
                ])
        };
        var settingsStore = new StubSettingsStore();
        var viewModel = CreateViewModel(apiClient, settingsStore);

        await viewModel.ConnectCommand.ExecuteAsync();

        Assert.Equal("API connection established", viewModel.StatusMessage);
        Assert.Equal("Administration", viewModel.ResourceTitle);
        Assert.Equal("Choose a management area.", viewModel.ResourceSummary);
        Assert.Equal("Inspect", Assert.Single(viewModel.AvailableActions));
        Assert.Equal("Messages", Assert.Single(viewModel.NavigationItems).Title);
        Assert.Equal(new ManagementSettings(viewModel.ApiBaseUrl, "en"), settingsStore.SavedSettings);
    }

    [Fact]
    public async Task OpenResourceAsyncFollowsSelectedSirenLink()
    {
        var apiClient = new StubApiClient
        {
            Resource = Resource("""{"title":"News","description":"Current bulletins"}""")
        };
        var viewModel = CreateViewModel(apiClient, new StubSettingsStore());
        viewModel.NavigationItems.Add(new NavigationItem("News", "/api/v1/news"));
        viewModel.SelectedNavigationItem = viewModel.NavigationItems[0];

        await viewModel.OpenResourceCommand.ExecuteAsync();

        Assert.Equal("/api/v1/news", apiClient.LastResourceHref);
        Assert.Equal("News", viewModel.ResourceTitle);
        Assert.Equal("Current bulletins", viewModel.ResourceSummary);
    }

    [Fact]
    public async Task ConnectAsyncLocalizesMachineReadableAuthorizationErrors()
    {
        var apiClient = new StubApiClient
        {
            DiscoveryException = new ManagementApiException(
                "errors.forbidden",
                HttpStatusCode.Forbidden)
        };
        var viewModel = CreateViewModel(
            apiClient,
            new StubSettingsStore(),
            new ManagementSettings(Locale: "it"));

        await viewModel.ConnectCommand.ExecuteAsync();

        Assert.Equal("Non hai l'autorizzazione per eseguire questa operazione.", viewModel.StatusMessage);
    }

    [Fact]
    public async Task ConnectAsyncMapsHttpFallbackErrorsToLocalizedStatus()
    {
        var apiClient = new StubApiClient
        {
            DiscoveryException = new ManagementApiException(
                "http.404",
                HttpStatusCode.NotFound)
        };
        var viewModel = CreateViewModel(apiClient, new StubSettingsStore());

        await viewModel.ConnectCommand.ExecuteAsync();

        Assert.Equal("The requested information could not be found.", viewModel.StatusMessage);
    }

    [Fact]
    public async Task ConnectAsyncShowsConnectionErrorWhenBackendIsUnavailable()
    {
        var apiClient = new StubApiClient
        {
            DiscoveryException = new HttpRequestException()
        };
        var viewModel = CreateViewModel(apiClient, new StubSettingsStore());

        await viewModel.ConnectCommand.ExecuteAsync();

        Assert.Equal("The network service is unavailable", viewModel.StatusMessage);
    }

    [Fact]
    public async Task ConnectAsyncReportsInvalidApiUrlAndSettingsWriteFailures()
    {
        var urlClient = new StubApiClient
        {
            DiscoveryException = new UriFormatException()
        };
        var urlViewModel = CreateViewModel(urlClient, new StubSettingsStore());
        await urlViewModel.ConnectCommand.ExecuteAsync();
        Assert.Equal("Enter a valid value.", urlViewModel.StatusMessage);

        var settingsStore = new StubSettingsStore { SaveException = new IOException() };
        var settingsViewModel = CreateViewModel(new StubApiClient(), settingsStore);
        await settingsViewModel.ConnectCommand.ExecuteAsync();
        Assert.Equal("Management settings could not be saved.", settingsViewModel.StatusMessage);
    }

    [Fact]
    public void LocaleChangePersistsPreferenceAndUpdatesLocalizedLabels()
    {
        var settingsStore = new StubSettingsStore();
        var viewModel = CreateViewModel(new StubApiClient(), settingsStore);

        viewModel.ApiBaseUrl = "http://localhost:5001/api";
        viewModel.ApiBaseUrl = "http://localhost:5001/api";
        viewModel.Locale = "it";

        Assert.Equal(1, viewModel.LocaleIndex);
        Assert.Equal("Connetti", viewModel.ConnectLabel);
        Assert.Equal("Sci-Fi LARP Network", viewModel.WindowTitle);
        Assert.Equal("Connessione al backend", viewModel.ConnectionHeading);
        Assert.Equal("Indirizzo API", viewModel.ApiAddressLabel);
        Assert.Equal("Lingua", viewModel.LanguageLabel);
        Assert.Equal("Navigazione", viewModel.NavigationHeading);
        Assert.Equal("Apri destinazione", viewModel.OpenResourceLabel);
        Assert.Equal("Azioni disponibili", viewModel.ActionsHeading);
        Assert.Equal(new ManagementSettings("http://localhost:5001/api", "it"), settingsStore.SavedSettings);
        viewModel.LocaleIndex = 0;
        Assert.Equal("en", viewModel.Locale);
    }

    [Theory]
    [InlineData("errors.validation", "The request contains invalid values.")]
    [InlineData("errors.unauthorized", "Sign in to continue.")]
    [InlineData("errors.forbidden", "You are not allowed to perform this action.")]
    [InlineData("errors.notFound", "The requested information could not be found.")]
    [InlineData("errors.conflict", "This change conflicts with the current data.")]
    [InlineData("errors.requestFailed", "The request could not be completed.")]
    [InlineData("errors.serviceUnavailable", "The service is temporarily unavailable.")]
    [InlineData("errors.internalServerError", "An unexpected server error occurred.")]
    [InlineData("http.invalid", "The request could not be completed.")]
    public async Task ConnectAsyncLocalizesApiErrorCodes(string errorCode, string expectedMessage)
    {
        var apiClient = new StubApiClient
        {
            DiscoveryException = new ManagementApiException(errorCode, HttpStatusCode.BadRequest)
        };
        var viewModel = CreateViewModel(apiClient, new StubSettingsStore());

        await viewModel.ConnectCommand.ExecuteAsync();

        Assert.Equal(expectedMessage, viewModel.StatusMessage);
    }

    [Fact]
    public void LocaleChangeReportsSettingsWriteFailure()
    {
        var settingsStore = new StubSettingsStore { SaveException = new IOException() };
        var viewModel = CreateViewModel(new StubApiClient(), settingsStore);

        viewModel.Locale = "it";

        Assert.Equal("Impossibile salvare le impostazioni di gestione.", viewModel.StatusMessage);
    }

    [Fact]
    public void ResourceWithoutDisplayPropertiesUsesSafeFallbackText()
    {
        var viewModel = CreateViewModel(new StubApiClient(), new StubSettingsStore());

        Assert.Equal("Sci-Fi LARP Network", viewModel.WindowTitle);
        Assert.Equal("Network workspace", viewModel.ResourceTitle);
        Assert.Equal("This service has not advertised any navigation destinations.", viewModel.ResourceSummary);
        Assert.False(viewModel.HasAvailableActions);
    }

    [Fact]
    public async Task ConnectCommandCannotRunConcurrently()
    {
        var completion = new TaskCompletionSource<ApiDiscovery>(
            TaskCreationOptions.RunContinuationsAsynchronously);
        var apiClient = new StubApiClient { DiscoveryTask = completion.Task };
        var viewModel = CreateViewModel(apiClient, new StubSettingsStore());

        var runningConnection = viewModel.ConnectCommand.ExecuteAsync();

        Assert.False(viewModel.ConnectCommand.CanExecute(null));
        Assert.False(viewModel.OpenResourceCommand.CanExecute(null));
        completion.SetResult(new ApiDiscovery(Resource("{}"), []));
        await runningConnection;
        Assert.True(viewModel.ConnectCommand.CanExecute(null));
    }

    [Fact]
    public async Task OpenResourceCommandDoesNothingWithoutSelection()
    {
        var apiClient = new StubApiClient();
        var viewModel = CreateViewModel(apiClient, new StubSettingsStore());

        await viewModel.OpenResourceCommand.ExecuteAsync();

        Assert.Null(apiClient.LastResourceHref);
    }

    private static MainWindowViewModel CreateViewModel(
        StubApiClient apiClient,
        StubSettingsStore settingsStore,
        ManagementSettings? settings = null) =>
        new(apiClient, settingsStore, new JsonLocalizer(), settings ?? new ManagementSettings());

    private static SirenEntity Resource(string properties, IReadOnlyList<SirenAction>? actions = null) =>
        new(
            ["resource"],
            JsonDocument.Parse(properties).RootElement.Clone(),
            [],
            actions);

    private sealed class StubSettingsStore : IManagementSettingsStore
    {
        public ManagementSettings? SavedSettings { get; private set; }

        public IOException? SaveException { get; init; }

        public ManagementSettings Load() => new();

        public void Save(ManagementSettings settings)
        {
            if (SaveException is not null)
            {
                throw SaveException;
            }

            SavedSettings = settings;
        }
    }

    private sealed class StubApiClient : IManagementApiClient
    {
        public ApiDiscovery Discovery { get; init; } =
            new(Resource("{}"), []);

        public Exception? DiscoveryException { get; init; }

        public Task<ApiDiscovery>? DiscoveryTask { get; init; }

        public SirenEntity Resource { get; init; } = MainWindowViewModelTests.Resource("{}");

        public string? LastResourceHref { get; private set; }

        public Task<ApiDiscovery> DiscoverAsync(
            string apiBaseUrl,
            string locale,
            CancellationToken cancellationToken = default)
        {
            if (DiscoveryException is not null)
            {
                return Task.FromException<ApiDiscovery>(DiscoveryException);
            }

            return DiscoveryTask ?? Task.FromResult(Discovery);
        }

        public Task<SirenEntity> GetResourceAsync(
            string apiBaseUrl,
            string href,
            string locale,
            CancellationToken cancellationToken = default)
        {
            LastResourceHref = href;
            return Task.FromResult(Resource);
        }

        public Task<SirenEntity> SubmitActionAsync(
            string apiBaseUrl,
            SirenAction action,
            JsonElement? body,
            string locale,
            CancellationToken cancellationToken = default) =>
            Task.FromResult(Resource);
    }
}
