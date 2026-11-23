using System.Text.Json;
using SciFiLarpNetwork.Common.Siren;
using SciFiLarpNetwork.Management.Api.Messages;

namespace SciFiLarpNetwork.Management.Api;

public interface IManagementApiClient
{
    Task<ManagementSession> SignInAsync(
        string apiBaseUrl,
        string userName,
        string password,
        string locale,
        CancellationToken cancellationToken = default);

    Task SignOutAsync(
        string apiBaseUrl,
        string locale,
        CancellationToken cancellationToken = default);

    Task<ManagementSession> RenewSessionAsync(
        string apiBaseUrl,
        string locale,
        CancellationToken cancellationToken = default);

    Task<ApiDiscovery> DiscoverAsync(
        string apiBaseUrl,
        string locale,
        CancellationToken cancellationToken = default);

    Task<SirenEntity> GetResourceAsync(
        string apiBaseUrl,
        string href,
        string locale,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Downloads a PNG, JPEG, or WebP image from an advertised link within the configured API origin and path.
    /// </summary>
    Task<byte[]> GetImageAsync(
        string apiBaseUrl,
        string href,
        string locale,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Downloads a file of <paramref name="mediaType"/>, such as a template, from an advertised link within the
    /// configured API origin and path.
    /// </summary>
    Task<byte[]> GetFileAsync(
        string apiBaseUrl,
        string href,
        string mediaType,
        string locale,
        CancellationToken cancellationToken = default);

    Task<SirenEntity> SubmitActionAsync(
        string apiBaseUrl,
        SirenAction action,
        JsonElement? body,
        string locale,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Submits an advertised JSON action whose successful response is a file of <paramref name="mediaType"/>, such as
    /// a PDF, and returns the file. Errors are read as Siren errors, as for <see cref="SubmitActionAsync"/>.
    /// </summary>
    Task<byte[]> DownloadActionAsync(
        string apiBaseUrl,
        SirenAction action,
        JsonElement body,
        string mediaType,
        string locale,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Listens to the live message updates advertised at <paramref name="href"/>, within the configured API origin and
    /// path, with the signed-in session, and reports them to <paramref name="handlers"/>. Disposing the result closes
    /// the connection without reporting it as lost. Clients without live updates listen to nothing.
    /// </summary>
    Task<IAsyncDisposable> ListenForMessagesAsync(
        string apiBaseUrl,
        string href,
        MessageLiveHandlers handlers,
        CancellationToken cancellationToken = default) =>
        Task.FromResult<IAsyncDisposable>(NoListening.Instance);

    private sealed class NoListening : IAsyncDisposable
    {
        public static readonly NoListening Instance = new();

        public ValueTask DisposeAsync() => ValueTask.CompletedTask;
    }
}
