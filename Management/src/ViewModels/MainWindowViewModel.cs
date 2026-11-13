using System.Collections.ObjectModel;
using System.Diagnostics;
using System.Net;
using System.Text.Json;
using SciFiLarpNetwork.Common.ApiLinkPolicy;
using SciFiLarpNetwork.Common.Siren;
using SciFiLarpNetwork.Management.Api;
using SciFiLarpNetwork.Management.Localization;
using SciFiLarpNetwork.Management.Settings;
using SciFiLarpNetwork.Management.ViewModels.Skills;

namespace SciFiLarpNetwork.Management.ViewModels;

public sealed class MainWindowViewModel : ObservableViewModel
{
    private readonly IManagementApiClient _apiClient;
    private readonly IManagementSettingsStore _settingsStore;
    private readonly IManagementLocalizer _localizer;
    private readonly ICatalogDialogService _catalogDialogs;
    private string _apiBaseUrl;
    private string _locale;
    private string _statusKey = "navigation.empty";
    private string? _apiErrorCode;
    private bool _isBusy;
    private bool _isAuthenticated;
    private string _userName = string.Empty;
    private string _password = string.Empty;
    private NavigationItem? _selectedNavigationItem;
    private SirenEntity? _selectedResource;
    private TalentCatalogViewModel? _talentCatalog;

    public MainWindowViewModel(
        IManagementApiClient apiClient,
        IManagementSettingsStore settingsStore,
        IManagementLocalizer localizer,
        ICatalogDialogService catalogDialogs,
        ManagementSettings settings,
        string? startupErrorKey = null)
    {
        _apiClient = apiClient;
        _settingsStore = settingsStore;
        _localizer = localizer;
        _catalogDialogs = catalogDialogs;
        _apiBaseUrl = settings.ApiBaseUrl ?? new ManagementSettings().ApiBaseUrl;
        _locale = settings.Locale is "en" or "it" ? settings.Locale : "en";
        ConnectCommand = new AsyncCommand(ConnectAsync, HandleCommandException, () => !IsBusy);
        SignInCommand = new AsyncCommand(SignInAsync, HandleCommandException, () => !IsBusy && !IsAuthenticated);
        SignOutCommand = new AsyncCommand(SignOutAsync, HandleCommandException, () => !IsBusy && IsAuthenticated);
        OpenResourceCommand = new AsyncCommand(
            OpenResourceAsync,
            HandleCommandException,
            () => !IsBusy && SelectedNavigationItem is not null);
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
            TalentCatalog?.Locale = normalized;
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

    public string ConnectLabel => _localizer.Translate("management.connect", Locale);

    public string UserNameLabel => _localizer.Translate("management.userName", Locale);

    public string PasswordLabel => _localizer.Translate("management.password", Locale);

    public string SignInLabel => _localizer.Translate("management.signIn", Locale);

    public string SignOutLabel => _localizer.Translate("management.signOut", Locale);

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

    /// <summary>The talents and skills screen, shown in place of the resource summary while open.</summary>
    public TalentCatalogViewModel? TalentCatalog
    {
        get => _talentCatalog;
        private set
        {
            if (_talentCatalog == value)
            {
                return;
            }

            if (_talentCatalog is not null)
            {
                _talentCatalog.SessionExpired -= OnCatalogSessionExpired;
            }

            _talentCatalog = value;
            if (_talentCatalog is not null)
            {
                _talentCatalog.SessionExpired += OnCatalogSessionExpired;
            }

            OnPropertyChanged();
            OnPropertyChanged(nameof(IsTalentCatalogOpen));
            OnPropertyChanged(nameof(IsResourceSummaryVisible));
        }
    }

    public bool IsTalentCatalogOpen => TalentCatalog is not null;

    public bool IsResourceSummaryVisible => TalentCatalog is null;

    private async Task ConnectAsync()
    {
        IsBusy = true;
        SetStatus("management.connecting");
        NavigationItems.Clear();
        AvailableActions.Clear();
        OnPropertyChanged(nameof(HasAvailableActions));
        SelectedNavigationItem = null;
        _selectedResource = null;
        TalentCatalog = null;
        OnPropertyChanged(nameof(ResourceTitle));
        OnPropertyChanged(nameof(ResourceSummary));

        try
        {
            _ = ApiLinkPolicy.CreateApiRoot(ApiBaseUrl);
            _settingsStore.Save(new ManagementSettings(ApiBaseUrl, Locale));
            var discovery = await _apiClient.DiscoverAsync(ApiBaseUrl, Locale);
            foreach (var link in discovery.NavigationLinks)
            {
                NavigationItems.Add(new NavigationItem(
                    link.Title!,
                    link.Href!,
                    link.Rel.Contains(SirenRelations.Talents, StringComparer.Ordinal)));
            }

            SetResource(discovery.ApiResource);
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
            await _apiClient.SignInAsync(ApiBaseUrl, UserName, Password, Locale);
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

        if (item.OpensTalentCatalog)
        {
            await OpenTalentCatalogAsync(item);
            return;
        }

        IsBusy = true;
        SetStatus("management.connecting");
        try
        {
            var resource = await _apiClient.GetResourceAsync(ApiBaseUrl, item.Href, Locale);
            TalentCatalog = null;
            SetResource(resource);
            SetStatus("app.apiConnected");
        }
        catch (ManagementApiException exception)
        {
            if (exception.StatusCode == HttpStatusCode.Unauthorized)
            {
                ClearSessionState();
            }

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
        catch (InvalidOperationException)
        {
            SetStatus("actions.unsupported");
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

    private async Task OpenTalentCatalogAsync(NavigationItem item)
    {
        IsBusy = true;
        try
        {
            var catalog = new TalentCatalogViewModel(_apiClient, _localizer, _catalogDialogs, ApiBaseUrl, Locale, item.Href);
            TalentCatalog = catalog;
            SetStatus("app.apiConnected");
            await catalog.LoadAsync();
        }
        finally
        {
            IsBusy = false;
        }
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
        IsAuthenticated = false;
        Password = string.Empty;
        NavigationItems.Clear();
        SelectedNavigationItem = null;
        AvailableActions.Clear();
        _selectedResource = null;
        TalentCatalog = null;
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
        OnPropertyChanged(nameof(LanguageLabel));
        OnPropertyChanged(nameof(LocaleIndex));
        OnPropertyChanged(nameof(NavigationHeading));
        OnPropertyChanged(nameof(ActionsHeading));
        OnPropertyChanged(nameof(ResourceTitle));
        OnPropertyChanged(nameof(ResourceSummary));
        OnPropertyChanged(nameof(StatusMessage));
    }
}
