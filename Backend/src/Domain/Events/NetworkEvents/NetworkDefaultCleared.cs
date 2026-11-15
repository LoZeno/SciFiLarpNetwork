namespace SciFiLarpNetwork.Backend.Domain.Events.NetworkEvents;

/// <summary>The network stopped being the default because another network was made default.</summary>
public sealed record NetworkDefaultCleared(Guid NetworkId, Guid NewDefaultNetworkId, DateTimeOffset OccurredAtUtc);
