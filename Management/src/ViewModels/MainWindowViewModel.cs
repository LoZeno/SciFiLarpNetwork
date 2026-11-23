using System.Collections.ObjectModel;
using System.Diagnostics;
using System.Globalization;
using System.Net;
using System.Text.Json;
using SciFiLarpNetwork.Common.ApiLinkPolicy;
using SciFiLarpNetwork.Common.Siren;
using SciFiLarpNetwork.Management.Api;
using SciFiLarpNetwork.Management.Localization;
using SciFiLarpNetwork.Management.Settings;
using SciFiLarpNetwork.Management.ViewModels.CardTemplates;
using SciFiLarpNetwork.Management.ViewModels.LarpEvents;
using SciFiLarpNetwork.Management.ViewModels.Messages;
using SciFiLarpNetwork.Management.ViewModels.Networks;
using SciFiLarpNetwork.Management.ViewModels.Players;
using SciFiLarpNetwork.Management.ViewModels.Skills;

namespace SciFiLarpNetwork.Management.ViewModels;

public sealed class MainWindowViewModel : ObservableViewModel
{
    private readonly IManagementApiClient _apiClient;
    private readonly IManagementSettingsStore _settingsStore;
    private readonly IManagementLocalizer _localizer;
    private readonly ICatalogDialogService _catalogDialogs;
    private readonly INetworkDialogService _networkDialogs;
    private readonly IPlayerDialogService _playerDialogs;
    private readonly ICardTemplateDialogService _cardTemplateDialogs;
    private readonly ILarpEventDialogService _eventDialogs;
    private readonly IMessageDialogService _messageDialogs;
    private string _apiBaseUrl;
    private string _locale;
    private string _statusKey = "navigation.empty";
    private string? _apiErrorCode;
    private bool _isBusy;
    private bool _isAuthenticated;
    private bool _isConnected;
    private string _userName = string.Empty;
    private string _password = string.Empty;
    private string _signedInAccount = string.Empty;
    private NavigationItem? _selectedNavigationItem;
    private SirenEntity? _selectedResource;
    private IManagementScreen? _currentScreen;
    private int _openGeneration;

    public MainWindowViewModel(
        IManagementApiClient apiClient,
        IManagementSettingsStore settingsStore,
        IManagementLocalizer localizer,
        ICatalogDialogService catalogDialogs,
        INetworkDialogService networkDialogs,
        IPlayerDialogService playerDialogs,
        ICardTemplateDialogService cardTemplateDialogs,
        ILarpEventDialogService eventDialogs,
        IMessageDialogService messageDialogs,
        ManagementSettings settings,
        string? startupErrorKey = null)
    {
        _apiClient = apiClient;
        _settingsStore = settingsStore;
        _localizer = localizer;
        _catalogDialogs = catalogDialogs;
        _networkDialogs = networkDialogs;
        _playerDialogs = playerDialogs;
        _cardTemplateDialogs = cardTemplateDialogs;
        _eventDialogs = eventDialogs;
        _messageDialogs = messageDialogs;
        _apiBaseUrl = settings.ApiBaseUrl ?? new ManagementSettings().ApiBaseUrl;
        _locale = settings.Locale is "en" or "it" ? settings.Locale : "en";
        ConnectCommand = new AsyncCommand(ConnectAsync, HandleCommandException, () => !IsBusy);
        SignInCommand = new AsyncCommand(SignInAsync, HandleCommandException, () => !IsBusy && !IsAuthenticated);
        SignOutCommand = new AsyncCommand(SignOutAsync, HandleCommandException, () => !IsBusy && IsAuthenticated);
        OpenResourceCommand = new AsyncCommand(
            OpenResourceAsync,
            HandleCommandException,
            () => SelectedNavigationItem is not null);
        SetStatus(startupErrorKey ?? "navigation.empty");
    }

    public AsyncCommand ConnectCommand { get; }

    public AsyncCommand SignInCommand { get; }

    public AsyncCommand SignOutCommand { get; }

    public AsyncCommand OpenResourceCommand { get; }

    public ObservableCollection<NavigationItem> NavigationItems { get; } = [];

    public ObservableCollection<string> AvailableActions { get; } = [];

    public string ApiBaseUrl
    {
        get => _apiBaseUrl;
        set
        {
            if (_apiBaseUrl == value)
            {
                return;
            }

            _apiBaseUrl = value;
            OnPropertyChanged();
        }
    }

    public string Locale
    {
        get => _locale;
        set
        {
            var normalized = value is "en" or "it" ? value : "en";
            if (_locale == normalized)
            {
                return;
            }

            _locale = normalized;
            OnPropertyChanged();
            OnPropertyChanged(nameof(LocaleIndex));
            OnLocalizedPropertiesChanged();
            CurrentScreen?.Locale = normalized;
            SaveSettings();
        }
    }

    public int LocaleIndex
    {
        get => Locale == "it" ? 1 : 0;
        set => Locale = value == 1 ? "it" : "en";
    }

    public NavigationItem? SelectedNavigationItem
    {
        get => _selectedNavigationItem;
        set
        {
            if (_selectedNavigationItem == value)
            {
                return;
            }

            _selectedNavigationItem = value;
            OnPropertyChanged();
            OpenResourceCommand.NotifyCanExecuteChanged();
            if (value is not null)
            {
                OpenResourceCommand.Execute(null);
            }
        }
    }

    public bool IsBusy
    {
        get => _isBusy;
        private set
        {
            if (_isBusy == value)
            {
                return;
            }

            _isBusy = value;
            OnPropertyChanged();
            ConnectCommand.NotifyCanExecuteChanged();
            SignInCommand.NotifyCanExecuteChanged();
            SignOutCommand.NotifyCanExecuteChanged();
            OpenResourceCommand.NotifyCanExecuteChanged();
        }
    }

    public bool IsAuthenticated
    {
        get => _isAuthenticated;
        private set
        {
            if (_isAuthenticated == value)
            {
                return;
            }

            _isAuthenticated = value;
            OnPropertyChanged();
            SignInCommand.NotifyCanExecuteChanged();
            SignOutCommand.NotifyCanExecuteChanged();
        }
    }

    public string UserName
    {
        get => _userName;
        set
        {
            if (_userName == value)
            {
                return;
            }

            _userName = value;
            OnPropertyChanged();
        }
    }

    public string Password
    {
        get => _password;
        set
        {
            if (_password == value)
            {
                return;
            }

            _password = value;
            OnPropertyChanged();
        }
    }

    public string WindowTitle => _localizer.Translate("app.name", Locale);

    public string ConnectionHeading => _localizer.Translate("management.connectionHeading", Locale);

    public string ApiAddressLabel => _localizer.Translate("management.apiAddress", Locale);

    /// <summary>True after a successful Connect, until the next Connect starts or the session ends.</summary>
    public bool IsConnected
    {
        get => _isConnected;
        private set
        {
            if (_isConnected == value)
            {
                return;
            }

            _isConnected = value;
            OnPropertyChanged();
            OnPropertyChanged(nameof(ConnectLabel));
        }
    }

    public string ConnectLabel => _localizer.Translate(IsConnected ? "management.reconnect" : "management.connect", Locale);

    public string UserNameLabel => _localizer.Translate("management.userName", Locale);

    public string PasswordLabel => _localizer.Translate("management.password", Locale);

    public string SignInLabel => _localizer.Translate("management.signIn", Locale);

    public string SignOutLabel => _localizer.Translate("management.signOut", Locale);

    public string SignedInAccountLabel => string.Format(
        CultureInfo.GetCultureInfo(Locale), _localizer.Translate("management.signedInAs", Locale), _signedInAccount);

    public string LanguageLabel => _localizer.Translate("common.language", Locale);

    public string NavigationHeading => _localizer.Translate("navigation.heading", Locale);

    public string ActionsHeading => _localizer.Translate("actions.heading", Locale);

    public string ResourceTitle => GetPropertyString("title")
        ?? GetPropertyString("name")
        ?? (_selectedResource?.Class is { Count: > 0 } classes ? classes[0] : null)
        ?? _localizer.Translate("app.workspace", Locale);

    public string ResourceSummary => GetPropertyString("description")
        ?? (NavigationItems.Count == 0
            ? _localizer.Translate("navigation.emptyPage", Locale)
            : _localizer.Translate("app.apiDescription", Locale));

    public string StatusMessage => _apiErrorCode is not null
        ? GetApiErrorMessage(_apiErrorCode)
        : _localizer.Translate(_statusKey, Locale);

    public bool HasAvailableActions => AvailableActions.Count > 0;

    /// <summary>The purpose-built screen shown in place of the resource summary, if one is open.</summary>
    public IManagementScreen? CurrentScreen
    {
        get => _currentScreen;
        private set
        {
            if (_currentScreen == value)
            {
                return;
            }

            if (_currentScreen is not null)
            {
                _currentScreen.SessionExpired -= OnCatalogSessionExpired;

                // A screen that holds a connection, such as live message updates, closes it when it is left.
                (_currentScreen as IDisposable)?.Dispose();
            }

            _currentScreen = value;
            if (_currentScreen is not null)
            {
                _currentScreen.SessionExpired += OnCatalogSessionExpired;
            }

            OnPropertyChanged();
            OnPropertyChanged(nameof(TalentCatalog));
            OnPropertyChanged(nameof(NetworkCatalog));
            OnPropertyChanged(nameof(IsTalentCatalogOpen));
            OnPropertyChanged(nameof(IsNetworkCatalogOpen));
            OnPropertyChanged(nameof(PlayerRoster));
            OnPropertyChanged(nameof(IsPlayerRosterOpen));
            OnPropertyChanged(nameof(CardTemplates));
            OnPropertyChanged(nameof(IsCardTemplatesOpen));
            OnPropertyChanged(nameof(Events));
            OnPropertyChanged(nameof(IsEventsOpen));
            OnPropertyChanged(nameof(Messages));
            OnPropertyChanged(nameof(IsMessagesOpen));
            OnPropertyChanged(nameof(IsResourceSummaryVisible));
        }
    }

    public PlayerRosterViewModel? PlayerRoster => CurrentScreen as PlayerRosterViewModel;

    public bool IsPlayerRosterOpen => PlayerRoster is not null;

    public TalentCatalogViewModel? TalentCatalog => CurrentScreen as TalentCatalogViewModel;

    public NetworkCatalogViewModel? NetworkCatalog => CurrentScreen as NetworkCatalogViewModel;

    public bool IsTalentCatalogOpen => TalentCatalog is not null;

    public bool IsNetworkCatalogOpen => NetworkCatalog is not null;

    public CardTemplateScreenViewModel? CardTemplates => CurrentScreen as CardTemplateScreenViewModel;

    public bool IsCardTemplatesOpen => CardTemplates is not null;

    public LarpEventScreenViewModel? Events => CurrentScreen as LarpEventScreenViewModel;

    public bool IsEventsOpen => Events is not null;

    public MessageScreenViewModel? Messages => CurrentScreen as MessageScreenViewModel;

    public bool IsMessagesOpen => Messages is not null;

    public bool IsResourceSummaryVisible => CurrentScreen is null;

    private async Task ConnectAsync()
    {
        IsBusy = true;
        ++_openGeneration;
        IsConnected = false;
        SetStatus("management.connecting");
        NavigationItems.Clear();
        AvailableActions.Clear();
        OnPropertyChanged(nameof(HasAvailableActions));
        SelectedNavigationItem = null;
        _selectedResource = null;
        CurrentScreen = null;
        OnPropertyChanged(nameof(ResourceTitle));
        OnPropertyChanged(nameof(ResourceSummary));

        try
        {
            _ = ApiLinkPolicy.CreateApiRoot(ApiBaseUrl);
            _settingsStore.Save(new ManagementSettings(ApiBaseUrl, Locale));
            var discovery = await _apiClient.DiscoverAsync(ApiBaseUrl, Locale);
            foreach (var link in discovery.NavigationLinks)
            {
                NavigationItems.Add(new NavigationItem(link.Title!, link.Href!, ScreenFor(link)));
            }

            SetResource(discovery.ApiResource);
            IsConnected = true;
            SetStatus("app.apiConnected");
        }
        catch (ManagementApiException exception)
        {
            _apiErrorCode = exception.ErrorCode;
            OnPropertyChanged(nameof(StatusMessage));
        }
        catch (HttpRequestException)
        {
            SetStatus("app.connectionError");
        }
        catch (UriFormatException)
        {
            SetStatus("validation.invalid");
        }
        catch (JsonException)
        {
            SetStatus("common.error");
        }
        catch (IOException)
        {
            SetStatus("management.settingsSaveError");
        }
        catch (UnauthorizedAccessException)
        {
            SetStatus("management.settingsSaveError");
        }
        finally
        {
            IsBusy = false;
        }
    }

    private async Task SignInAsync()
    {
        IsBusy = true;
        SetStatus("management.signingIn");
        try
        {
            var session = await _apiClient.SignInAsync(ApiBaseUrl, UserName, Password, Locale);
            _signedInAccount = session.UserName;
            OnPropertyChanged(nameof(SignedInAccountLabel));
            Password = string.Empty;
            IsAuthenticated = true;
            SetStatus("management.signedIn");
        }
        catch (ManagementApiException exception)
        {
            _apiErrorCode = exception.ErrorCode;
            OnPropertyChanged(nameof(StatusMessage));
        }
        catch (HttpRequestException)
        {
            SetStatus("app.connectionError");
        }
        catch (UriFormatException)
        {
            SetStatus("validation.invalid");
        }
        catch (JsonException)
        {
            SetStatus("common.error");
        }
        finally
        {
            IsBusy = false;
        }
    }

    private async Task SignOutAsync()
    {
        IsBusy = true;
        try
        {
            await _apiClient.SignOutAsync(ApiBaseUrl, Locale);
            SetStatus("management.signedOut");
        }
        catch (ManagementApiException exception)
        {
            _apiErrorCode = exception.ErrorCode;
            OnPropertyChanged(nameof(StatusMessage));
        }
        catch (HttpRequestException)
        {
            SetStatus("app.connectionError");
        }
        finally
        {
            ClearSessionState();
            IsBusy = false;
        }
    }

    private async Task OpenResourceAsync()
    {
        if (SelectedNavigationItem is not { } item)
        {
            return;
        }

        var generation = ++_openGeneration;
        var opened = false;
        try
        {
            opened = item.Screen == NavigationScreen.ResourceSummary
                ? await OpenGenericResourceAsync(item, generation)
                : await OpenScreenAsync(CreateScreen(item));
        }
        finally
        {
            if (generation == _openGeneration)
            {
                ClearFailedSelection(item, opened);
            }
        }
    }

    /// <summary>
    /// Clears the selection after a failed open, so selecting the same entry again retries it.
    /// </summary>
    private void ClearFailedSelection(NavigationItem item, bool opened)
    {
        if (!opened && SelectedNavigationItem == item)
        {
            SelectedNavigationItem = null;
        }
    }

    private async Task<bool> OpenGenericResourceAsync(NavigationItem item, int generation)
    {
        try
        {
            var resource = await _apiClient.GetResourceAsync(ApiBaseUrl, item.Href, Locale);
            if (generation == _openGeneration)
            {
                CurrentScreen = null;
                SetResource(resource);
                SetStatus("app.apiConnected");
            }

            return true;
        }
        catch (Exception failure) when (failure is ManagementApiException or HttpRequestException or UriFormatException
                                            or InvalidOperationException or JsonException)
        {
            if (generation == _openGeneration)
            {
                ReportOpenFailure(failure);
            }

            return false;
        }
    }

    private void ReportOpenFailure(Exception failure)
    {
        switch (failure)
        {
            case ManagementApiException exception:
                if (exception.StatusCode == HttpStatusCode.Unauthorized)
                {
                    ClearSessionState();
                }

                _apiErrorCode = exception.ErrorCode;
                OnPropertyChanged(nameof(StatusMessage));
                break;
            case HttpRequestException:
                SetStatus("app.connectionError");
                break;
            case UriFormatException:
                SetStatus("validation.invalid");
                break;
            case InvalidOperationException:
                SetStatus("actions.unsupported");
                break;
            default:
                SetStatus("common.error");
                break;
        }
    }

    private static NavigationScreen ScreenFor(SirenLink link) =>
        link.Rel.Contains(SirenRelations.Talents, StringComparer.Ordinal) ? NavigationScreen.TalentCatalog
        : link.Rel.Contains(SirenRelations.Networks, StringComparer.Ordinal) ? NavigationScreen.NetworkCatalog
        : link.Rel.Contains(SirenRelations.Players, StringComparer.Ordinal) ? NavigationScreen.PlayerRoster
        : link.Rel.Contains(SirenRelations.CardTemplates, StringComparer.Ordinal) ? NavigationScreen.CardTemplates
        : link.Rel.Contains(SirenRelations.Events, StringComparer.Ordinal) ? NavigationScreen.Events
        : link.Rel.Contains(SirenRelations.Messages, StringComparer.Ordinal) ? NavigationScreen.Messages
        : NavigationScreen.ResourceSummary;

    private IManagementScreen CreateScreen(NavigationItem item) =>
        item.Screen switch
        {
            NavigationScreen.TalentCatalog =>
                new TalentCatalogViewModel(_apiClient, _localizer, _catalogDialogs, ApiBaseUrl, Locale, item.Href),
            NavigationScreen.NetworkCatalog =>
                new NetworkCatalogViewModel(_apiClient, _localizer, _networkDialogs, ApiBaseUrl, Locale, item.Href),
            NavigationScreen.PlayerRoster =>
                new PlayerRosterViewModel(_apiClient, _localizer, _playerDialogs, ApiBaseUrl, Locale, item.Href),
            NavigationScreen.CardTemplates =>
                new CardTemplateScreenViewModel(_apiClient, _localizer, _cardTemplateDialogs, ApiBaseUrl, Locale, item.Href),
            NavigationScreen.Events =>
                new LarpEventScreenViewModel(_apiClient, _localizer, _eventDialogs, ApiBaseUrl, Locale, item.Href),
            NavigationScreen.Messages =>
                new MessageScreenViewModel(_apiClient, _localizer, _messageDialogs, ApiBaseUrl, Locale, item.Href),
            _ => throw new ArgumentOutOfRangeException(nameof(item), item.Screen, "The entry opens the resource summary.")
        };

    private async Task<bool> OpenScreenAsync(IManagementScreen screen)
    {
        CurrentScreen = screen;
        SetStatus("app.apiConnected");
        await screen.LoadAsync();
        return screen.IsLoaded;
    }

    private void OnCatalogSessionExpired(object? sender, EventArgs e)
    {
        ClearSessionState();
        SetStatus("errors.unauthorized");
    }

    private void SetResource(SirenEntity resource)
    {
        _selectedResource = resource;
        AvailableActions.Clear();
        foreach (var action in resource.Actions ?? [])
        {
            var title = action.Title ?? action.Name;
            if (!string.IsNullOrWhiteSpace(title))
            {
                AvailableActions.Add(title);
            }
        }

        OnPropertyChanged(nameof(ResourceTitle));
        OnPropertyChanged(nameof(ResourceSummary));
        OnPropertyChanged(nameof(HasAvailableActions));
    }

    private void ClearSessionState()
    {
        ++_openGeneration;
        IsAuthenticated = false;
        IsConnected = false;
        Password = string.Empty;
        NavigationItems.Clear();
        SelectedNavigationItem = null;
        AvailableActions.Clear();
        _selectedResource = null;
        CurrentScreen = null;
        OnPropertyChanged(nameof(ResourceTitle));
        OnPropertyChanged(nameof(ResourceSummary));
        OnPropertyChanged(nameof(HasAvailableActions));
    }

    private string? GetPropertyString(string name)
    {
        if (_selectedResource?.Properties is not { ValueKind: JsonValueKind.Object } properties ||
            !properties.TryGetProperty(name, out var value) ||
            value.ValueKind != JsonValueKind.String)
        {
            return null;
        }

        return value.GetString();
    }

    private string GetApiErrorMessage(string errorCode) =>
        _localizer.TranslateApiError(errorCode, Locale);

    private void SaveSettings()
    {
        try
        {
            _ = ApiLinkPolicy.CreateApiRoot(ApiBaseUrl);
            _settingsStore.Save(new ManagementSettings(ApiBaseUrl, Locale));
        }
        catch (UriFormatException)
        {
            SetStatus("validation.invalid");
        }
        catch (IOException)
        {
            SetStatus("management.settingsSaveError");
        }
        catch (UnauthorizedAccessException)
        {
            SetStatus("management.settingsSaveError");
        }
    }

    private void SetStatus(string statusKey)
    {
        _statusKey = statusKey;
        _apiErrorCode = null;
        OnPropertyChanged(nameof(StatusMessage));
    }

    private void HandleCommandException(Exception exception)
    {
        Trace.TraceError("Unhandled management command exception: {0}", exception);
        SetStatus("common.error");
    }

    private void OnLocalizedPropertiesChanged()
    {
        OnPropertyChanged(nameof(WindowTitle));
        OnPropertyChanged(nameof(ConnectionHeading));
        OnPropertyChanged(nameof(ApiAddressLabel));
        OnPropertyChanged(nameof(ConnectLabel));
        OnPropertyChanged(nameof(UserNameLabel));
        OnPropertyChanged(nameof(PasswordLabel));
        OnPropertyChanged(nameof(SignInLabel));
        OnPropertyChanged(nameof(SignOutLabel));
        OnPropertyChanged(nameof(SignedInAccountLabel));
        OnPropertyChanged(nameof(LanguageLabel));
        OnPropertyChanged(nameof(LocaleIndex));
        OnPropertyChanged(nameof(NavigationHeading));
        OnPropertyChanged(nameof(ActionsHeading));
        OnPropertyChanged(nameof(ResourceTitle));
        OnPropertyChanged(nameof(ResourceSummary));
        OnPropertyChanged(nameof(StatusMessage));
    }
}
