namespace SciFiLarpNetwork.Backend.Application.Players;

/// <summary>
/// Administrator-defined character stats. New stats are defined through character commands; this service lists,
/// renames, and archives them. Character values are stored by stat identifier, so a rename changes no character.
/// </summary>
public interface ICharacterStatService
{
    Task<IReadOnlyList<CharacterStatSummary>> GetStatsAsync(bool includeArchived,
        CancellationToken cancellationToken = default);

    Task<CharacterStatSummary?> GetStatAsync(Guid statId, CancellationToken cancellationToken = default);

    Task<RosterResult<CharacterStatSummary>> RenameStatAsync(
        Guid statId,
        long expectedVersion,
        string name,
        CancellationToken cancellationToken = default);

    Task<RosterResult<CharacterStatSummary>> ArchiveStatAsync(Guid statId, CancellationToken cancellationToken = default);

    Task<RosterResult<CharacterStatSummary>> UnarchiveStatAsync(Guid statId,
        CancellationToken cancellationToken = default);
}
