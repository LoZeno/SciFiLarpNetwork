namespace SciFiLarpNetwork.Backend.Domain.Events.NetworkEvents;

/// <summary>
/// Appended to the single network-default stream on every default change, so concurrent default changes conflict
/// on that stream's version.
/// </summary>
public sealed record DefaultNetworkChosen(Guid NetworkId, DateTimeOffset OccurredAtUtc);
