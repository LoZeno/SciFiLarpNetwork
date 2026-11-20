using System.Text.Json;
using SciFiLarpNetwork.Common.Siren;

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
}
