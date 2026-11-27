namespace SciFiLarpNetwork.Backend.Application.Items;

/// <summary>
/// The rarity catalog (legacy “availability”). Rarities are listed in an administrator-set order; a new rarity goes
/// last. They are archived, never deleted, and an archived rarity keeps its place.
/// </summary>
public interface IRarityService
{
    Task<IReadOnlyList<RaritySummary>> GetRaritiesAsync(bool includeArchived, CancellationToken cancellationToken = default);

    Task<RaritySummary?> GetRarityAsync(Guid rarityId, CancellationToken cancellationToken = default);

    Task<ItemResult<RaritySummary>> CreateRarityAsync(string name, CancellationToken cancellationToken = default);

    /// <summary>
    /// Renames the rarity if it is still at <paramref name="expectedVersion"/>; otherwise reports
    /// <see cref="ItemOutcome.StaleVersion"/> and changes nothing.
    /// </summary>
    Task<ItemResult<RaritySummary>> RenameRarityAsync(Guid rarityId, long expectedVersion, string name,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Swaps the rarity with the nearest rarity above (<paramref name="up"/>) or below that is not archived; archived
    /// rarities in between keep their places. A rarity with no such neighbour stays where it is.
    /// </summary>
    Task<ItemResult<RaritySummary>> MoveRarityAsync(Guid rarityId, bool up, CancellationToken cancellationToken = default);

    Task<ItemResult<RaritySummary>> ArchiveRarityAsync(Guid rarityId, CancellationToken cancellationToken = default);

    Task<ItemResult<RaritySummary>> UnarchiveRarityAsync(Guid rarityId, CancellationToken cancellationToken = default);
}
