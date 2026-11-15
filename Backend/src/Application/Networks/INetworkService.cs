namespace SciFiLarpNetwork.Backend.Application.Networks;

/// <summary>
/// The network catalog: the configurable in-game networks characters belong to and players select.
/// </summary>
public interface INetworkService
{
    Task<IReadOnlyList<NetworkSummary>> GetNetworksAsync(bool includeDisabled,
        CancellationToken cancellationToken = default);

    Task<NetworkSummary?> GetNetworkAsync(Guid networkId, CancellationToken cancellationToken = default);

    Task<NetworkResult> CreateNetworkAsync(string code, NetworkDefinition definition,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Replaces the network's details if it is still at <paramref name="expectedVersion"/>; otherwise reports
    /// <see cref="NetworkOutcome.StaleVersion"/> and changes nothing.
    /// </summary>
    Task<NetworkResult> UpdateNetworkAsync(
        Guid networkId,
        long expectedVersion,
        NetworkDefinition definition,
        CancellationToken cancellationToken = default);

    Task<NetworkResult> EnableNetworkAsync(Guid networkId, CancellationToken cancellationToken = default);

    /// <summary>Disables the network; the default network cannot be disabled.</summary>
    Task<NetworkResult> DisableNetworkAsync(Guid networkId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Makes the enabled network the only default, moving the default from the previous network in the same command.
    /// </summary>
    Task<NetworkResult> MakeDefaultNetworkAsync(Guid networkId, CancellationToken cancellationToken = default);
}
