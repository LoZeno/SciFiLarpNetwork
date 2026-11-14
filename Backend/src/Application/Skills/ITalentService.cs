namespace SciFiLarpNetwork.Backend.Application.Skills;

/// <summary>
/// The talent catalog: groups (categories) of skills, called “attitudini” in Italian.
/// </summary>
public interface ITalentService
{
    Task<IReadOnlyList<TalentSummary>> GetTalentsAsync(bool includeArchived, CancellationToken cancellationToken = default);

    Task<TalentSummary?> GetTalentAsync(Guid talentId, CancellationToken cancellationToken = default);

    Task<CatalogResult<TalentSummary>> CreateTalentAsync(TalentDefinition definition, CancellationToken cancellationToken = default);

    /// <summary>
    /// Replaces the talent's details if it is still at <paramref name="expectedVersion"/>; otherwise reports
    /// <see cref="CatalogOutcome.StaleVersion"/> and changes nothing.
    /// </summary>
    Task<CatalogResult<TalentSummary>> UpdateTalentAsync(
        Guid talentId,
        long expectedVersion,
        TalentDefinition definition,
        CancellationToken cancellationToken = default);

    Task<CatalogResult<TalentSummary>> ArchiveTalentAsync(Guid talentId, CancellationToken cancellationToken = default);

    Task<CatalogResult<TalentSummary>> UnarchiveTalentAsync(Guid talentId, CancellationToken cancellationToken = default);
}
