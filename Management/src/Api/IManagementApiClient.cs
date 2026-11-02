using System.Text.Json;
using SciFiLarpNetwork.Common.Siren;

namespace SciFiLarpNetwork.Management.Api;

public interface IManagementApiClient
{
    Task<ApiDiscovery> DiscoverAsync(
        string apiBaseUrl,
        string locale,
        CancellationToken cancellationToken = default);

    Task<SirenEntity> GetResourceAsync(
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
}
