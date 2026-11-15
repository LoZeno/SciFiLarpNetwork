using SciFiLarpNetwork.Backend.Application.Networks;
using SciFiLarpNetwork.Backend.Infrastructure.Networks.NetworkDocuments;

namespace SciFiLarpNetwork.Backend.Infrastructure.Networks;

internal static class NetworkMappings
{
    extension(NetworkDocument network)
    {
        public NetworkSummary ToSummary() =>
            new(
                network.Id,
                network.Code,
                network.Name,
                network.Description,
                network.Theme,
                network is { BackgroundImageId: { } imageId, BackgroundImageMediaType: { } mediaType }
                    ? new NetworkImageReference(imageId, mediaType, network.BackgroundImageByteCount)
                    : null,
                network.IsEnabled,
                network.IsDefault,
                network.Version);
    }

    extension(NetworkResult)
    {
        public static NetworkResult Success(NetworkDocument network) =>
            new(NetworkOutcome.Succeeded, network.ToSummary());

        public static NetworkResult Failure(NetworkOutcome outcome) => new(outcome, null);
    }
}
