namespace SciFiLarpNetwork.Backend.Application.Networks;

/// <summary>
/// The outcome of a network command. <see cref="Value"/> holds the resulting state when the command succeeded.
/// </summary>
public sealed record NetworkResult(NetworkOutcome Outcome, NetworkSummary? Value);
