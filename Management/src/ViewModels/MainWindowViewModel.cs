using System.Collections.ObjectModel;
using System.Diagnostics;
using System.Net;
using System.Net.Http;
using System.Text.Json;
using SciFiLarpNetwork.Management.Api;
using SciFiLarpNetwork.Management.Localization;
using SciFiLarpNetwork.Management.Settings;

namespace SciFiLarpNetwork.Management.ViewModels;

public sealed class MainWindowViewModel : ObservableViewModel
{
    private readonly IManagementApiClient _apiClient;
    private readonly IManagementSettingsStore _settingsStore;
    private readonly IManagementLocalizer _localizer;
    private string _apiBaseUrl;
    private string _locale;
    private string _statusKey = "navigation.empty";
    private string? _apiErrorCode;
    private bool _isBusy;
    private NavigationItem? _selectedNavigationItem;
    private SirenEntity? _selectedResource;

    public MainWindowViewModel(
        IManagementApiClient apiClient,
        IManagementSettingsStore settingsStore,
        IManagementLocalizer localizer,
        ManagementSettings settings,
        string? startupErrorKey = null)
    {
        _apiClient = apiClient;
        _settingsStore = settingsStore;
        _localizer = localizer;
        _apiBaseUrl = settings.ApiBaseUrl ?? new ManagementSettings().ApiBaseUrl;
        _locale = settings.Locale is "en" or "it" ? settings.Locale : "en";
        ConnectCommand = new AsyncCommand(ConnectAsync, HandleCommandException, () => !IsBusy);
        OpenResourceCommand = new AsyncCommand(
            OpenResourceAsync,
            HandleCommandException,
            () => !IsBusy && SelectedNavigationItem is not null);
        SetStatus(startupErrorKey ?? "navigation.empty");
    }

    public AsyncCommand ConnectCommand { get; }

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
            OpenResourceCommand.NotifyCanExecuteChanged();
        }
    }

    public string WindowTitle => _localizer.Translate("app.name", Locale);

    public string ConnectionHeading => _localizer.Translate("management.connectionHeading", Locale);

    public string ApiAddressLabel => _localizer.Translate("management.apiAddress", Locale);

    public string ConnectLabel => _localizer.Translate("management.connect", Locale);

    public string LanguageLabel => _localizer.Translate("common.language", Locale);

    public string NavigationHeading => _localizer.Translate("navigation.heading", Locale);

    public string OpenResourceLabel => _localizer.Translate("management.openResource", Locale);

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

    private async Task ConnectAsync()
    {
        IsBusy = true;
        SetStatus("management.connecting");
        NavigationItems.Clear();
        AvailableActions.Clear();
        OnPropertyChanged(nameof(HasAvailableActions));
        SelectedNavigationItem = null;
        _selectedResource = null;
        OnPropertyChanged(nameof(ResourceTitle));
        OnPropertyChanged(nameof(ResourceSummary));

        try
        {
            _settingsStore.Save(new ManagementSettings(ApiBaseUrl, Locale));
            var discovery = await _apiClient.DiscoverAsync(ApiBaseUrl, Locale);
            foreach (var link in discovery.NavigationLinks)
            {
                NavigationItems.Add(new NavigationItem(link.Title!, link.Href!));
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

    private async Task OpenResourceAsync()
    {
        if (SelectedNavigationItem is not { } item)
        {
            return;
        }

        IsBusy = true;
        SetStatus("management.connecting");
        try
        {
            var resource = await _apiClient.GetResourceAsync(ApiBaseUrl, item.Href, Locale);
            SetResource(resource);
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

    private string GetApiErrorMessage(string errorCode)
    {
        var key = errorCode switch
        {
            "errors.validation" => "errors.validation",
            "errors.unauthorized" => "errors.unauthorized",
            "errors.forbidden" => "errors.forbidden",
            "errors.notFound" => "errors.notFound",
            "errors.conflict" => "errors.conflict",
            "errors.requestFailed" => "errors.requestFailed",
            "errors.serviceUnavailable" => "errors.serviceUnavailable",
            "errors.internalServerError" => "errors.internalServerError",
            _ when TryGetStatusCode(errorCode, out var statusCode) => statusCode switch
            {
                HttpStatusCode.Unauthorized => "errors.unauthorized",
                HttpStatusCode.Forbidden => "errors.forbidden",
                HttpStatusCode.NotFound => "errors.notFound",
                HttpStatusCode.Conflict => "errors.conflict",
                HttpStatusCode.ServiceUnavailable => "errors.serviceUnavailable",
                >= HttpStatusCode.InternalServerError => "errors.internalServerError",
                _ => "errors.requestFailed"
            },
            _ => "errors.requestFailed"
        };
        return _localizer.Translate(key, Locale);
    }

    private static bool TryGetStatusCode(string errorCode, out HttpStatusCode statusCode)
    {
        statusCode = default;
        if (!errorCode.StartsWith("http.", StringComparison.Ordinal) ||
            !int.TryParse(errorCode.AsSpan("http.".Length), out var numericStatus))
        {
            return false;
        }

        var parsedStatus = (HttpStatusCode)numericStatus;
        if (!Enum.IsDefined(parsedStatus))
        {
            return false;
        }

        statusCode = parsedStatus;
        return true;
    }

    private void SaveSettings()
    {
        try
        {
            _settingsStore.Save(new ManagementSettings(ApiBaseUrl, Locale));
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
        OnPropertyChanged(nameof(LanguageLabel));
        OnPropertyChanged(nameof(LocaleIndex));
        OnPropertyChanged(nameof(NavigationHeading));
        OnPropertyChanged(nameof(OpenResourceLabel));
        OnPropertyChanged(nameof(ActionsHeading));
        OnPropertyChanged(nameof(ResourceTitle));
        OnPropertyChanged(nameof(ResourceSummary));
        OnPropertyChanged(nameof(StatusMessage));
    }
}
