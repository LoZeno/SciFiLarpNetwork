using System.Collections.ObjectModel;
using System.Diagnostics;
using System.Net;
using System.Text.Json;
using SciFiLarpNetwork.Common.Siren;
using SciFiLarpNetwork.Management.Api;
using SciFiLarpNetwork.Management.Localization;

namespace SciFiLarpNetwork.Management.ViewModels;

/// <summary>
/// Shared behavior of the purpose-built screens: following advertised links and actions, the busy state, localized
/// status and error messages, and reporting an expired session. Screens follow only what the API advertises, so
/// readers see no change commands and the server still authorizes and validates every change.
/// </summary>
public abstract class ScreenViewModel : ObservableViewModel, IManagementScreen
{
    private const string StaleVersionCode = "errors.staleVersion";
    private readonly IManagementApiClient _apiClient;
    private readonly string _apiBaseUrl;
    private string _locale;
    private bool _isBusy;
    private bool _canWrite;
    private string? _statusMessage;
    private int _runningOperations;

    private protected ScreenViewModel(IManagementApiClient apiClient, IManagementLocalizer localizer, string apiBaseUrl, string locale)
    {
        _apiClient = apiClient;
        Localizer = localizer;
        _apiBaseUrl = apiBaseUrl;
        _locale = locale;
    }

    /// <inheritdoc />
    public event EventHandler? SessionExpired;

    public string Locale
    {
        get => _locale;
        set
        {
            if (_locale == value)
            {
                return;
            }

            _locale = value;
            OnPropertyChanged();
            OnLocaleChanged();
        }
    }

    public bool CanWrite
    {
        get => _canWrite;
        private protected set
        {
            if (_canWrite == value)
            {
                return;
            }

            _canWrite = value;
            OnPropertyChanged();
            NotifyCommandsChanged();
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
            NotifyCommandsChanged();
        }
    }

    public string? StatusMessage
    {
        get => _statusMessage;
        private protected set
        {
            if (_statusMessage == value)
            {
                return;
            }

            _statusMessage = value;
            OnPropertyChanged();
            OnPropertyChanged(nameof(HasStatusMessage));
        }
    }

    public bool HasStatusMessage => !string.IsNullOrEmpty(StatusMessage);

    /// <inheritdoc />
    public bool IsLoaded { get; private protected set; }

    private protected IManagementApiClient ApiClient => _apiClient;

    private protected string ApiBaseUrl => _apiBaseUrl;

    private protected IManagementLocalizer Localizer { get; }

    public abstract Task LoadAsync();

    /// <summary>Called after the locale changes, to relabel the screen and reload localized data.</summary>
    private protected abstract void OnLocaleChanged();

    /// <summary>Called when the busy or write state changes, so commands can re-evaluate whether they may run.</summary>
    private protected abstract void NotifyCommandsChanged();

    private protected string Translate(string key) => Localizer.Translate(key, Locale);

    private protected Task<SirenEntity> GetAsync(string href) =>
        _apiClient.GetResourceAsync(_apiBaseUrl, href, Locale);

    private protected Task<SirenEntity> SubmitAsync(SirenAction action, JsonElement? body) =>
        _apiClient.SubmitActionAsync(_apiBaseUrl, action, body, Locale);

    /// <summary>Runs a screen operation, keeping the screen busy until every running operation has finished.</summary>
    private protected async Task RunAsync(Func<Task> operation)
    {
        _runningOperations++;
        IsBusy = true;
        StatusMessage = null;
        try
        {
            await operation();
        }
        catch (Exception failure) when (IsExpectedFailure(failure))
        {
            StatusMessage = Report(failure);
        }
        finally
        {
            _runningOperations--;
            IsBusy = _runningOperations > 0;
        }
    }

    /// <summary>
    /// Submits a dialog's change. A failure is shown in the dialog, which stays open; after a stale-version conflict
    /// <paramref name="reloadAfterConflict"/> loads the latest values into the dialog.
    /// </summary>
    private protected async Task<bool> SubmitEditorAsync(
        CatalogEditorViewModel editor,
        Func<Task> submit,
        Func<Task>? reloadAfterConflict)
    {
        try
        {
            await submit();
            return true;
        }
        catch (Exception failure) when (IsExpectedFailure(failure))
        {
            var message = Report(failure);
            if (failure is ManagementApiException { ErrorCode: StaleVersionCode } && reloadAfterConflict is not null)
            {
                message = await TryReloadAfterConflictAsync(reloadAfterConflict) ?? message;
            }

            editor.ErrorMessage = message;
            return false;
        }
    }

    private protected static bool IsExpectedFailure(Exception failure) =>
        failure is ManagementApiException or HttpRequestException or JsonException or InvalidOperationException
            or UriFormatException;

    private protected string Report(Exception failure)
    {
        switch (failure)
        {
            case ManagementApiException apiFailure:
                if (apiFailure.StatusCode == HttpStatusCode.Unauthorized)
                {
                    SessionExpired?.Invoke(this, EventArgs.Empty);
                }

                return Localizer.TranslateApiError(apiFailure.ErrorCode, Locale);
            case HttpRequestException:
                return Translate("app.connectionError");
            case UriFormatException:
                return Translate("validation.invalid");
            case InvalidOperationException:
                return Translate("actions.unsupported");
            default:
                return Translate("common.error");
        }
    }

    private protected static InvalidOperationException MissingAction() =>
        new("The API did not advertise the link or action this screen needs.");

    private protected void HandleUnexpectedFailure(Exception exception)
    {
        Trace.TraceError("Unhandled management screen exception: {0}", exception);
        StatusMessage = Translate("common.error");
    }

    private protected static void ReplaceAll<T>(ObservableCollection<T> collection, IEnumerable<T> items)
    {
        collection.Clear();
        foreach (var item in items)
        {
            collection.Add(item);
        }
    }

    private async Task<string?> TryReloadAfterConflictAsync(Func<Task> reload)
    {
        try
        {
            await reload();
            return null;
        }
        catch (Exception failure) when (IsExpectedFailure(failure))
        {
            return Report(failure);
        }
    }
}
