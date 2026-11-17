namespace SciFiLarpNetwork.Backend.Application.Players;

/// <summary>
/// The outcome of a player, character, or stat command. <see cref="Value"/> holds the resulting state when the
/// command succeeded.
/// </summary>
public sealed record RosterResult<T>(RosterOutcome Outcome, T? Value)
    where T : class;
