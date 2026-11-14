namespace SciFiLarpNetwork.Backend.Application.Skills;

/// <summary>
/// The skill catalog. A skill can belong to several talents and can require other skills.
/// </summary>
public interface ISkillService
{
    /// <param name="talentId">When set, only skills assigned to this talent are returned.</param>
    Task<IReadOnlyList<SkillSummary>> GetSkillsAsync(
        Guid? talentId,
        bool includeArchived,
        CancellationToken cancellationToken = default);

    Task<SkillSummary?> GetSkillAsync(Guid skillId, CancellationToken cancellationToken = default);

    Task<CatalogResult<SkillSummary>> CreateSkillAsync(SkillDefinition definition, CancellationToken cancellationToken = default);

    /// <summary>
    /// Replaces the skill's state if it is still at <paramref name="expectedVersion"/>; otherwise reports
    /// <see cref="CatalogOutcome.StaleVersion"/> and changes nothing.
    /// </summary>
    Task<CatalogResult<SkillSummary>> UpdateSkillAsync(
        Guid skillId,
        long expectedVersion,
        SkillDefinition definition,
        CancellationToken cancellationToken = default);

    Task<CatalogResult<SkillSummary>> ArchiveSkillAsync(Guid skillId, CancellationToken cancellationToken = default);

    Task<CatalogResult<SkillSummary>> UnarchiveSkillAsync(Guid skillId, CancellationToken cancellationToken = default);
}
