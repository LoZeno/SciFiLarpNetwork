using JasperFx;
using JasperFx.Events;
using Marten;
using SciFiLarpNetwork.Backend.Application.Skills;
using SciFiLarpNetwork.Backend.Domain.Events.CardFlagEvents;
using SciFiLarpNetwork.Backend.Domain.Events.SkillEvents;
using SciFiLarpNetwork.Backend.Domain.Skills;
using SciFiLarpNetwork.Backend.Infrastructure.Persistence.EventStore;
using SciFiLarpNetwork.Backend.Infrastructure.Skills.SkillDocuments;

namespace SciFiLarpNetwork.Backend.Infrastructure.Skills;

/// <summary>
/// Skill commands follow Marten's command handler workflow: one session, <c>FetchForWriting</c> on every stream
/// the decision depends on, and one <c>SaveChangesAsync</c>. Streams that are read but not appended to are
/// marked <c>AlwaysEnforceConsistency</c>, so a concurrent change to them (for example archiving a talent, or
/// another edit that would close a prerequisite loop) rejects the commit instead of being overlooked.
/// </summary>
internal sealed class SkillService(AttributedSessionFactory sessions, TimeProvider timeProvider) : ISkillService
{
    private const int MaximumCost = 999;

    public async Task<IReadOnlyList<SkillSummary>> GetSkillsAsync(
        Guid? talentId,
        bool includeArchived,
        CancellationToken cancellationToken = default)
    {
        await using var query = sessions.QuerySession();
        IQueryable<SkillDocument> skills = query.Query<SkillDocument>();
        if (talentId is { } talent)
        {
            skills = skills.Where(skill => skill.TalentIds.Contains(talent));
        }

        if (!includeArchived)
        {
            skills = skills.Where(skill => !skill.IsArchived);
        }

        return (await skills.ToListAsync(cancellationToken))
            .OrderBy(skill => skill.NormalizedName, StringComparer.Ordinal)
            .Select(skill => skill.ToSummary())
            .ToArray();
    }

    public async Task<SkillSummary?> GetSkillAsync(Guid skillId, CancellationToken cancellationToken = default)
    {
        await using var query = sessions.QuerySession();
        return (await query.LoadAsync<SkillDocument>(skillId, cancellationToken))?.ToSummary();
    }

    public async Task<CatalogResult<SkillSummary>> CreateSkillAsync(SkillDefinition definition, CancellationToken cancellationToken = default)
    {
        if (ValidateDetails(definition) is var detailsOutcome and not CatalogOutcome.Succeeded)
        {
            return CatalogResult<SkillSummary>.Failure(detailsOutcome);
        }

        var skillId = Guid.NewGuid();
        var groups = ToGroups(definition.PrerequisiteGroups);
        if (ToOutcome(SkillPrerequisites.FindStructuralProblem(skillId, groups)) is var structureOutcome and not CatalogOutcome.Succeeded)
        {
            return CatalogResult<SkillSummary>.Failure(structureOutcome);
        }

        await using var session = sessions.OpenSession();
        var normalizedName = CatalogNames.Normalize(definition.Name);
        if (await IsNameUsedAsync(session, normalizedName, exceptSkillId: null, cancellationToken))
        {
            return CatalogResult<SkillSummary>.Failure(CatalogOutcome.NameAlreadyUsed);
        }

        var talentIds = definition.TalentIds.Distinct().ToArray();
        if (await LockAssignableTalentsAsync(session, talentIds, cancellationToken) is var talentOutcome and not CatalogOutcome.Succeeded)
        {
            return CatalogResult<SkillSummary>.Failure(talentOutcome);
        }

        var now = timeProvider.GetUtcNow();
        var (cardFlagOutcome, cardFlagIds) = await ResolveCardFlagsAsync(session, definition, now, cancellationToken);
        if (cardFlagOutcome != CatalogOutcome.Succeeded)
        {
            return CatalogResult<SkillSummary>.Failure(cardFlagOutcome);
        }

        // A new skill cannot be required by any existing skill, so it cannot close a loop; its prerequisites only need to exist.
        if (!await AllSkillsExistAsync(session, SkillPrerequisites.ReferencedSkills(groups), cancellationToken))
        {
            return CatalogResult<SkillSummary>.Failure(CatalogOutcome.UnknownPrerequisite);
        }

        session.Events.StartStream<SkillDocument>(
            skillId,
            new SkillCreated(
                skillId,
                definition.Name.Trim(),
                normalizedName,
                CatalogText.NormalizeDescription(definition.Description),
                definition.Cost,
                definition.IsMultiPurchase,
                definition.IsAdvanced,
                talentIds,
                ToGameFlags(definition.GameFlags),
                cardFlagIds,
                groups,
                now));
        return await SaveAsync(session, skillId, cancellationToken);
    }

    public async Task<CatalogResult<SkillSummary>> UpdateSkillAsync(
        Guid skillId,
        long expectedVersion,
        SkillDefinition definition,
        CancellationToken cancellationToken = default)
    {
        if (ValidateDetails(definition) is var detailsOutcome and not CatalogOutcome.Succeeded)
        {
            return CatalogResult<SkillSummary>.Failure(detailsOutcome);
        }

        var groups = ToGroups(definition.PrerequisiteGroups);
        if (ToOutcome(SkillPrerequisites.FindStructuralProblem(skillId, groups)) is var structureOutcome and not CatalogOutcome.Succeeded)
        {
            return CatalogResult<SkillSummary>.Failure(structureOutcome);
        }

        await using var session = sessions.OpenSession();
        var (fetchOutcome, stream) = await FetchExpectedAsync(session, skillId, expectedVersion, cancellationToken);
        if (stream?.Aggregate is not { } current)
        {
            return CatalogResult<SkillSummary>.Failure(fetchOutcome);
        }

        var normalizedName = CatalogNames.Normalize(definition.Name);
        if (await IsNameUsedAsync(session, normalizedName, skillId, cancellationToken))
        {
            return CatalogResult<SkillSummary>.Failure(CatalogOutcome.NameAlreadyUsed);
        }

        var talentIds = definition.TalentIds.Distinct().ToArray();
        var newlyAssignedTalents = talentIds.Except(current.TalentIds).ToArray();
        if (await LockAssignableTalentsAsync(session, newlyAssignedTalents, cancellationToken) is var talentOutcome and not CatalogOutcome.Succeeded)
        {
            return CatalogResult<SkillSummary>.Failure(talentOutcome);
        }

        var now = timeProvider.GetUtcNow();
        var (cardFlagOutcome, cardFlagIds) = await ResolveCardFlagsAsync(session, definition, now, cancellationToken);
        if (cardFlagOutcome != CatalogOutcome.Succeeded)
        {
            return CatalogResult<SkillSummary>.Failure(cardFlagOutcome);
        }

        var prerequisitesChanged = !SameGroups(current.PrerequisiteGroups, groups);
        if (prerequisitesChanged &&
            await LockPrerequisiteGraphAsync(session, skillId, groups, cancellationToken) is var prerequisiteOutcome and not CatalogOutcome.Succeeded)
        {
            return CatalogResult<SkillSummary>.Failure(prerequisiteOutcome);
        }

        var details = new SkillDetailsChanged(
            skillId,
            definition.Name.Trim(),
            normalizedName,
            CatalogText.NormalizeDescription(definition.Description),
            definition.Cost,
            definition.IsMultiPurchase,
            definition.IsAdvanced,
            now);
        if (details.Name != current.Name ||
            details.Description != current.Description ||
            details.Cost != current.Cost ||
            details.IsMultiPurchase != current.IsMultiPurchase ||
            details.IsAdvanced != current.IsAdvanced)
        {
            stream.AppendOne(details);
        }

        if (!talentIds.ToHashSet().SetEquals(current.TalentIds))
        {
            stream.AppendOne(new SkillTalentsChanged(skillId, talentIds, now));
        }

        var gameFlags = ToGameFlags(definition.GameFlags);
        if (!gameFlags.ToHashSet().SetEquals(current.GameFlags))
        {
            stream.AppendOne(new SkillGameFlagsChanged(skillId, gameFlags, now));
        }

        if (!cardFlagIds.ToHashSet().SetEquals(current.CardFlagIds))
        {
            stream.AppendOne(new SkillCardFlagsChanged(skillId, cardFlagIds, now));
        }

        if (prerequisitesChanged)
        {
            stream.AppendOne(new SkillPrerequisitesChanged(skillId, groups, now));
        }

        return await SaveAsync(session, skillId, cancellationToken);
    }

    public Task<CatalogResult<SkillSummary>> ArchiveSkillAsync(Guid skillId, CancellationToken cancellationToken = default) =>
        SetArchivedAsync(skillId, archived: true, cancellationToken);

    public Task<CatalogResult<SkillSummary>> UnarchiveSkillAsync(Guid skillId, CancellationToken cancellationToken = default) =>
        SetArchivedAsync(skillId, archived: false, cancellationToken);

    private async Task<CatalogResult<SkillSummary>> SetArchivedAsync(Guid skillId, bool archived, CancellationToken cancellationToken)
    {
        await using var session = sessions.OpenSession();
        var stream = await session.Events.FetchForWriting<SkillDocument>(skillId, cancellationToken);
        if (stream.Aggregate is not { } current)
        {
            return CatalogResult<SkillSummary>.Failure(CatalogOutcome.NotFound);
        }

        if (current.IsArchived == archived)
        {
            return CatalogResult<SkillSummary>.Success(current.ToSummary());
        }

        var now = timeProvider.GetUtcNow();
        stream.AppendOne(archived ? new SkillArchived(skillId, now) : new SkillUnarchived(skillId, now));
        return await SaveAsync(session, skillId, cancellationToken);
    }

    private static CatalogOutcome ValidateDetails(SkillDefinition definition)
    {
        if (!CatalogNames.IsValid(definition.Name))
        {
            return CatalogOutcome.InvalidName;
        }

        return definition.Cost is < 0 or > MaximumCost ? CatalogOutcome.InvalidCost : CatalogOutcome.Succeeded;
    }

    private static CatalogOutcome ToOutcome(PrerequisiteProblem problem) =>
        problem switch
        {
            PrerequisiteProblem.EmptyGroup => CatalogOutcome.EmptyPrerequisiteGroup,
            PrerequisiteProblem.SelfReference => CatalogOutcome.SelfPrerequisite,
            PrerequisiteProblem.DuplicateInGroup => CatalogOutcome.DuplicatePrerequisite,
            _ => CatalogOutcome.Succeeded
        };

    private static Guid[][] ToGroups(IReadOnlyList<IReadOnlyList<Guid>> groups) =>
        groups.Select(group => group.ToArray()).ToArray();

    private static GameFlag[] ToGameFlags(IReadOnlyList<GameFlag> gameFlags) =>
        gameFlags.Distinct().Order().ToArray();

    private static bool SameGroups(Guid[][] current, Guid[][] requested) =>
        current.Length == requested.Length &&
        current.Zip(requested).All(pair => pair.First.SequenceEqual(pair.Second));

    private static async Task<(CatalogOutcome Outcome, IEventStream<SkillDocument>? Stream)> FetchExpectedAsync(
        IDocumentSession session,
        Guid skillId,
        long expectedVersion,
        CancellationToken cancellationToken)
    {
        try
        {
            var stream = await session.Events.FetchForWriting<SkillDocument>(skillId, expectedVersion, cancellationToken);
            return stream.Aggregate is null ? (CatalogOutcome.NotFound, null) : (CatalogOutcome.Succeeded, stream);
        }
        catch (ConcurrencyException)
        {
            return await session.LoadAsync<SkillDocument>(skillId, cancellationToken) is null
                ? (CatalogOutcome.NotFound, null)
                : (CatalogOutcome.StaleVersion, null);
        }
    }

    private static Task<bool> IsNameUsedAsync(
        IQuerySession session,
        string normalizedName,
        Guid? exceptSkillId,
        CancellationToken cancellationToken)
    {
        var sameName = session.Query<SkillDocument>().Where(skill => skill.NormalizedName == normalizedName);
        return exceptSkillId is { } excluded
            ? sameName.Where(skill => skill.Id != excluded).AnyAsync(cancellationToken)
            : sameName.AnyAsync(cancellationToken);
    }

    private static async Task<CatalogOutcome> LockAssignableTalentsAsync(
        IDocumentSession session,
        Guid[] talentIds,
        CancellationToken cancellationToken)
    {
        if (talentIds.Length == 0)
        {
            return CatalogOutcome.Succeeded;
        }

        foreach (var talent in await session.Events.FetchManyForWriting<TalentDocument>(talentIds, cancellationToken))
        {
            switch (talent.Aggregate)
            {
                case null:
                    return CatalogOutcome.UnknownTalent;
                case { IsArchived: true }:
                    return CatalogOutcome.TalentArchived;
                default:
                    talent.AlwaysEnforceConsistency = true;
                    break;
            }
        }

        return CatalogOutcome.Succeeded;
    }

    private static async Task<(CatalogOutcome Outcome, Guid[] CardFlagIds)> ResolveCardFlagsAsync(
        IDocumentSession session,
        SkillDefinition definition,
        DateTimeOffset now,
        CancellationToken cancellationToken)
    {
        var selected = definition.CardFlagIds.Distinct().ToList();
        if (selected.Count > 0 && (await session.LoadManyAsync<CardFlagDocument>(cancellationToken, selected.ToArray())).Count != selected.Count)
        {
            return (CatalogOutcome.UnknownCardFlag, []);
        }

        if (definition.NewCardFlagNames.Any(name => !CatalogNames.IsValid(name)))
        {
            return (CatalogOutcome.InvalidCardFlagName, []);
        }

        foreach (var name in definition.NewCardFlagNames.DistinctBy(CatalogNames.Normalize))
        {
            var normalizedName = CatalogNames.Normalize(name);
            var existing = await session.Query<CardFlagDocument>()
                .Where(cardFlag => cardFlag.NormalizedName == normalizedName)
                .FirstOrDefaultAsync(cancellationToken);
            if (existing is not null)
            {
                selected.Add(existing.Id);
                continue;
            }

            var cardFlagId = Guid.NewGuid();
            session.Events.StartStream<CardFlagDocument>(cardFlagId, new CardFlagDefined(cardFlagId, name.Trim(), normalizedName, now));
            selected.Add(cardFlagId);
        }

        return (CatalogOutcome.Succeeded, selected.Distinct().ToArray());
    }

    private static async Task<bool> AllSkillsExistAsync(
        IQuerySession session,
        IReadOnlySet<Guid> skillIds,
        CancellationToken cancellationToken) =>
        skillIds.Count == 0 ||
        (await session.LoadManyAsync<SkillDocument>(cancellationToken, skillIds.ToArray())).Count == skillIds.Count;

    /// <summary>
    /// Loads, for writing, every skill reachable from the requested prerequisites and checks that the edited skill is
    /// not among them. Each loaded stream enforces consistency, so a concurrent prerequisite change anywhere in the
    /// graph that was read rejects this commit.
    /// </summary>
    private static async Task<CatalogOutcome> LockPrerequisiteGraphAsync(
        IDocumentSession session,
        Guid skillId,
        Guid[][] groups,
        CancellationToken cancellationToken)
    {
        var prerequisitesBySkill = new Dictionary<Guid, IReadOnlyList<IReadOnlyList<Guid>>>();
        var directPrerequisites = SkillPrerequisites.ReferencedSkills(groups);
        var frontier = directPrerequisites.Where(id => id != skillId).ToArray();
        while (frontier.Length > 0)
        {
            var streams = await session.Events.FetchManyForWriting<SkillDocument>(frontier, cancellationToken);
            for (var index = 0; index < frontier.Length; index++)
            {
                var stream = streams[index];
                if (stream.Aggregate is null && directPrerequisites.Contains(frontier[index]))
                {
                    return CatalogOutcome.UnknownPrerequisite;
                }

                stream.AlwaysEnforceConsistency = true;
                prerequisitesBySkill[frontier[index]] = stream.Aggregate?.PrerequisiteGroups ?? [];
            }

            frontier = frontier
                .SelectMany(id => SkillPrerequisites.ReferencedSkills(prerequisitesBySkill[id]))
                .Where(id => id != skillId && !prerequisitesBySkill.ContainsKey(id))
                .Distinct()
                .ToArray();
        }

        var createsLoop = SkillPrerequisites.CreatesLoop(
            skillId,
            groups,
            id => prerequisitesBySkill.TryGetValue(id, out var prerequisites) ? prerequisites : []);
        return createsLoop ? CatalogOutcome.PrerequisiteLoop : CatalogOutcome.Succeeded;
    }

    private static async Task<CatalogResult<SkillSummary>> SaveAsync(IDocumentSession session, Guid skillId, CancellationToken cancellationToken)
    {
        try
        {
            if (!await session.TrySaveChangesAsync(cancellationToken))
            {
                return CatalogResult<SkillSummary>.Failure(CatalogOutcome.StaleVersion);
            }
        }
        catch (DocumentAlreadyExistsException)
        {
            // A unique index on NormalizedName rejected a concurrent command that claimed the same skill or card flag name.
            return CatalogResult<SkillSummary>.Failure(CatalogOutcome.NameAlreadyUsed);
        }

        var saved = await session.LoadAsync<SkillDocument>(skillId, cancellationToken);
        return CatalogResult<SkillSummary>.Success(saved!.ToSummary());
    }
}
