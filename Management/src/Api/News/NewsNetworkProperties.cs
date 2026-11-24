namespace SciFiLarpNetwork.Management.Api.News;

/// <param name="IsKnown">Whether a network has this identifier; otherwise the name is missing.</param>
internal sealed record NewsNetworkProperties(Guid NetworkId, bool IsKnown, string? Name, bool IsEnabled);
