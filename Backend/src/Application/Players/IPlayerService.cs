namespace SciFiLarpNetwork.Backend.Application.Players;

/// <summary>Players: the real participants who control characters. They are not application accounts.</summary>
public interface IPlayerService
{
    Task<RosterPage<PlayerSummary>> GetPlayersAsync(RosterQuery query, CancellationToken cancellationToken = default);

    Task<PlayerSummary?> GetPlayerAsync(Guid playerId, CancellationToken cancellationToken = default);

    /// <summary>Registers an Active player with the next player running number.</summary>
    Task<RosterResult<PlayerSummary>> CreatePlayerAsync(PlayerDefinition definition,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Replaces the player's details if the player is still at <paramref name="expectedVersion"/>; otherwise reports
    /// <see cref="RosterOutcome.StaleVersion"/> and changes nothing.
    /// </summary>
    Task<RosterResult<PlayerSummary>> UpdatePlayerAsync(
        Guid playerId,
        long expectedVersion,
        PlayerDefinition definition,
        CancellationToken cancellationToken = default);

    /// <summary>Makes the player Inactive and, in the same command, every Active character of the player Retired.</summary>
    Task<RosterResult<PlayerSummary>> DeactivatePlayerAsync(Guid playerId, CancellationToken cancellationToken = default);

    /// <summary>Makes the player Active again. Retired characters stay Retired.</summary>
    Task<RosterResult<PlayerSummary>> ActivatePlayerAsync(Guid playerId, CancellationToken cancellationToken = default);

    /// <summary>Archives an Inactive player.</summary>
    Task<RosterResult<PlayerSummary>> ArchivePlayerAsync(Guid playerId, CancellationToken cancellationToken = default);

    Task<RosterResult<PlayerSummary>> UnarchivePlayerAsync(Guid playerId, CancellationToken cancellationToken = default);
}
