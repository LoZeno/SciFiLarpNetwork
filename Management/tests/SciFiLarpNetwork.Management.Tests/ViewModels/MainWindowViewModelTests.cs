using System.Net;
using System.Net.Http;
using System.Text.Json;
using SciFiLarpNetwork.Management.Api;
using SciFiLarpNetwork.Management.Localization;
using SciFiLarpNetwork.Management.Settings;
using SciFiLarpNetwork.Management.ViewModels;
using SciFiLarpNetwork.Common.Siren;

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
                    [new SirenAction
                    {
                        Name = "inspect",
                        Href = "/api/v1",
                        Method = "GET",
                        Title = "Inspect"
                    }]),
                [
                    new SirenLink
                    {
                        Rel = [SirenRelations.Navigation],
                        Href = "/api/v1/messages",
                        Title = "Messages"
                    }
                ])
        };
        var settingsStore = new StubSettingsStore();
        var viewModel = CreateViewModel(apiClient, settingsStore);

        await viewModel.ConnectCommand.ExecuteAsync();

        Assert.Equal(
            new
            {
                Status = "API connection established",
                Title = "Administration",
                Summary = "Choose a management area.",
                Actions = "Inspect",
                Navigation = (NavigationItem?)new NavigationItem("Messages", "/api/v1/messages"),
                Settings = (ManagementSettings?)new ManagementSettings(viewModel.ApiBaseUrl, "en")
            },
            new
            {
                Status = viewModel.StatusMessage,
                Title = viewModel.ResourceTitle,
                Summary = viewModel.ResourceSummary,
                Actions = string.Join(',', viewModel.AvailableActions),
                Navigation = viewModel.NavigationItems.SingleOrDefault(),
                Settings = settingsStore.SavedSettings
            });
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

        Assert.Equal(
            new { Href = "/api/v1/news", Title = "News", Summary = "Current bulletins" },
            new
            {
                Href = apiClient.LastResourceHref ?? string.Empty,
                Title = viewModel.ResourceTitle,
                Summary = viewModel.ResourceSummary
            });
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
    public async Task ConnectAsyncReportsInvalidApiUrl()
    {
        var urlClient = new StubApiClient
        {
            DiscoveryException = new UriFormatException()
        };
        var urlViewModel = CreateViewModel(urlClient, new StubSettingsStore());
        await urlViewModel.ConnectCommand.ExecuteAsync();

        Assert.Equal("Enter a valid value.", urlViewModel.StatusMessage);
    }

    [Fact]
    public async Task ConnectAsyncReportsSettingsWriteFailure()
    {
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
        viewModel.Locale = "it";

        Assert.Equal(
            new
            {
                LocaleIndex = 1,
                ConnectLabel = "Connetti",
                WindowTitle = "Sci-Fi LARP Network",
                ConnectionHeading = "Connessione al backend",
                ApiAddressLabel = "Indirizzo API",
                LanguageLabel = "Lingua",
                NavigationHeading = "Navigazione",
                OpenResourceLabel = "Apri destinazione",
                ActionsHeading = "Azioni disponibili",
                Settings = (ManagementSettings?)new ManagementSettings("http://localhost:5001/api", "it")
            },
            new
            {
                viewModel.LocaleIndex,
                viewModel.ConnectLabel,
                viewModel.WindowTitle,
                viewModel.ConnectionHeading,
                viewModel.ApiAddressLabel,
                viewModel.LanguageLabel,
                viewModel.NavigationHeading,
                viewModel.OpenResourceLabel,
                viewModel.ActionsHeading,
                Settings = settingsStore.SavedSettings
            });
    }

    [Theory]
    [InlineData("it", 0, "en")]
    [InlineData("en", 1, "it")]
    public void LocaleIndexUpdatesTheSelectedLocale(string initialLocale, int localeIndex, string expectedLocale)
    {
        var viewModel = CreateViewModel(
            new StubApiClient(),
            new StubSettingsStore(),
            new ManagementSettings(Locale: initialLocale));

        viewModel.LocaleIndex = localeIndex;

        Assert.Equal(expectedLocale, viewModel.Locale);
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

        Assert.Equal(
            new
            {
                WindowTitle = "Sci-Fi LARP Network",
                ResourceTitle = "Network workspace",
                ResourceSummary = "This service has not advertised any navigation destinations.",
                HasAvailableActions = false
            },
            new
            {
                viewModel.WindowTitle,
                viewModel.ResourceTitle,
                viewModel.ResourceSummary,
                viewModel.HasAvailableActions
            });
    }

    [Fact]
    public async Task ConnectCommandCannotRunConcurrently()
    {
        var completion = new TaskCompletionSource<ApiDiscovery>(
            TaskCreationOptions.RunContinuationsAsynchronously);
        var apiClient = new StubApiClient { DiscoveryTask = completion.Task };
        var viewModel = CreateViewModel(apiClient, new StubSettingsStore());

        var runningConnection = viewModel.ConnectCommand.ExecuteAsync();

        var canConnectWhileRunning = viewModel.ConnectCommand.CanExecute(null);
        var canOpenResourceWhileRunning = viewModel.OpenResourceCommand.CanExecute(null);
        completion.SetResult(new ApiDiscovery(Resource("{}"), []));
        await runningConnection;
        var canConnectAfterCompletion = viewModel.ConnectCommand.CanExecute(null);

        Assert.Equal(
            new { CanConnectWhileRunning = false, CanOpenWhileRunning = false, CanConnectAfterCompletion = true },
            new
            {
                CanConnectWhileRunning = canConnectWhileRunning,
                CanOpenWhileRunning = canOpenResourceWhileRunning,
                CanConnectAfterCompletion = canConnectAfterCompletion
            });
    }

    [Fact]
    public async Task SignInCommandClearsPasswordAndDoesNotPersistSessionCredentials()
    {
        var apiClient = new StubApiClient();
        var settingsStore = new StubSettingsStore();
        var viewModel = CreateViewModel(apiClient, settingsStore);
        viewModel.UserName = "operator";
        viewModel.Password = "a-long-local-password";

        await viewModel.SignInCommand.ExecuteAsync();

        Assert.Equal(
            new
            {
                IsAuthenticated = true,
                Password = string.Empty,
                SubmittedPassword = "a-long-local-password",
                SavedSettings = (ManagementSettings?)null
            },
            new
            {
                viewModel.IsAuthenticated,
                viewModel.Password,
                SubmittedPassword = apiClient.LastSignInPassword ?? string.Empty,
                SavedSettings = settingsStore.SavedSettings
            });
    }

    [Fact]
    public async Task CredentialBearingApiAddressIsNotSaved()
    {
        var settingsStore = new StubSettingsStore();
        var viewModel = CreateViewModel(new StubApiClient(), settingsStore);
        viewModel.ApiBaseUrl = "http://user:secret@localhost:5000/api";

        await viewModel.ConnectCommand.ExecuteAsync();

        Assert.Null(settingsStore.SavedSettings);
    }

    [Fact]
    public void CredentialBearingApiAddressShowsLocalizedValidationErrorOnLocaleChange()
    {
        var viewModel = CreateViewModel(new StubApiClient(), new StubSettingsStore());
        viewModel.ApiBaseUrl = "http://user:secret@localhost:5000/api";

        viewModel.Locale = "it";

        Assert.Equal("Inserisci un valore valido.", viewModel.StatusMessage);
    }

    [Fact]
    public async Task ExpiredSessionClearsAuthenticatedUiState()
    {
        var apiClient = new StubApiClient
        {
            ResourceException = new ManagementApiException("errors.unauthorized", HttpStatusCode.Unauthorized)
        };
        var viewModel = CreateViewModel(apiClient, new StubSettingsStore());
        await viewModel.SignInCommand.ExecuteAsync();
        viewModel.NavigationItems.Add(new NavigationItem("Messages", "/api/v1/messages"));
        viewModel.SelectedNavigationItem = viewModel.NavigationItems[0];

        await viewModel.OpenResourceCommand.ExecuteAsync();

        Assert.Equal(
            new { IsAuthenticated = false, NavigationCount = 0, Status = "Sign in to continue." },
            new
            {
                viewModel.IsAuthenticated,
                NavigationCount = viewModel.NavigationItems.Count,
                Status = viewModel.StatusMessage
            });
    }

    [Fact]
    public async Task SignOutClearsSessionAndProtectedNavigation()
    {
        var viewModel = CreateViewModel(new StubApiClient(), new StubSettingsStore());
        await viewModel.SignInCommand.ExecuteAsync();
        viewModel.NavigationItems.Add(new NavigationItem("Accounts", "/api/v1/admin/accounts"));
        viewModel.SelectedNavigationItem = viewModel.NavigationItems[0];

        await viewModel.SignOutCommand.ExecuteAsync();

        Assert.Equal(
            new
            {
                IsAuthenticated = false,
                NavigationCount = 0,
                SelectedNavigationItem = (NavigationItem?)null
            },
            new
            {
                viewModel.IsAuthenticated,
                NavigationCount = viewModel.NavigationItems.Count,
                SelectedNavigationItem = (NavigationItem?)viewModel.SelectedNavigationItem
            });
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
        new()
        {
            Class = ["resource"],
            Properties = JsonDocument.Parse(properties).RootElement.Clone(),
            Entities = [],
            Actions = actions
        };

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

        public Exception? ResourceException { get; init; }

        public Exception? SignInException { get; init; }

        public Task<ApiDiscovery>? DiscoveryTask { get; init; }

        public SirenEntity Resource { get; init; } = MainWindowViewModelTests.Resource("{}");

        public string? LastResourceHref { get; private set; }

        public string? LastSignInPassword { get; private set; }

        public Task<ManagementSession> SignInAsync(
            string apiBaseUrl,
            string userName,
            string password,
            string locale,
            CancellationToken cancellationToken = default)
        {
            LastSignInPassword = password;
            return SignInException is not null
                ? Task.FromException<ManagementSession>(SignInException)
                : Task.FromResult(new ManagementSession(userName, "administrator", DateTimeOffset.UtcNow.AddHours(8)));
        }

        public Task SignOutAsync(
            string apiBaseUrl,
            string locale,
            CancellationToken cancellationToken = default) => Task.CompletedTask;

        public Task<ManagementSession> RenewSessionAsync(
            string apiBaseUrl,
            string locale,
            CancellationToken cancellationToken = default) =>
            Task.FromResult(new ManagementSession("operator", "administrator", DateTimeOffset.UtcNow.AddHours(8)));

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
            return ResourceException is null
                ? Task.FromResult(Resource)
                : Task.FromException<SirenEntity>(ResourceException);
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
