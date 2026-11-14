using JasperFx;
using JasperFx.Events;
using Marten;
using SciFiLarpNetwork.Backend.Application.Skills;
using SciFiLarpNetwork.Backend.Domain.Events.TalentEvents;
using SciFiLarpNetwork.Backend.Domain.Skills;
using SciFiLarpNetwork.Backend.Infrastructure.Persistence.EventStore;
using SciFiLarpNetwork.Backend.Infrastructure.Skills.SkillDocuments;

namespace SciFiLarpNetwork.Backend.Infrastructure.Skills;

internal sealed class TalentService(AttributedSessionFactory sessions, TimeProvider timeProvider) : ITalentService
{
    public async Task<IReadOnlyList<TalentSummary>> GetTalentsAsync(bool includeArchived, CancellationToken cancellationToken = default)
    {
        await using var query = sessions.QuerySession();
        var talents = includeArchived
            ? await query.Query<TalentDocument>().ToListAsync(cancellationToken)
            : await query.Query<TalentDocument>().Where(talent => !talent.IsArchived).ToListAsync(cancellationToken);
        return talents
            .OrderBy(talent => talent.NormalizedName, StringComparer.Ordinal)
            .Select(talent => talent.ToSummary())
            .ToArray();
    }

    public async Task<TalentSummary?> GetTalentAsync(Guid talentId, CancellationToken cancellationToken = default)
    {
        await using var query = sessions.QuerySession();
        return (await query.LoadAsync<TalentDocument>(talentId, cancellationToken))?.ToSummary();
    }

    public async Task<CatalogResult<TalentSummary>> CreateTalentAsync(TalentDefinition definition, CancellationToken cancellationToken = default)
    {
        if (!CatalogNames.IsValid(definition.Name))
        {
            return CatalogResult<TalentSummary>.Failure(CatalogOutcome.InvalidName);
        }

        await using var session = sessions.OpenSession();
        var normalizedName = CatalogNames.Normalize(definition.Name);
        if (await IsNameUsedAsync(session, normalizedName, exceptTalentId: null, cancellationToken))
        {
            return CatalogResult<TalentSummary>.Failure(CatalogOutcome.NameAlreadyUsed);
        }

        var talentId = Guid.NewGuid();
        session.Events.StartStream<TalentDocument>(
            talentId,
            new TalentCreated(talentId, definition.Name.Trim(), normalizedName, CatalogText.NormalizeDescription(definition.Description), timeProvider.GetUtcNow()));
        return await SaveAsync(session, talentId, cancellationToken);
    }

    public async Task<CatalogResult<TalentSummary>> UpdateTalentAsync(
        Guid talentId,
        long expectedVersion,
        TalentDefinition definition,
        CancellationToken cancellationToken = default)
    {
        if (!CatalogNames.IsValid(definition.Name))
        {
            return CatalogResult<TalentSummary>.Failure(CatalogOutcome.InvalidName);
        }

        await using var session = sessions.OpenSession();
        var (outcome, stream) = await FetchExpectedAsync(session, talentId, expectedVersion, cancellationToken);
        if (stream?.Aggregate is not { } current)
        {
            return CatalogResult<TalentSummary>.Failure(outcome);
        }

        var normalizedName = CatalogNames.Normalize(definition.Name);
        if (await IsNameUsedAsync(session, normalizedName, talentId, cancellationToken))
        {
            return CatalogResult<TalentSummary>.Failure(CatalogOutcome.NameAlreadyUsed);
        }

        var name = definition.Name.Trim();
        var description = CatalogText.NormalizeDescription(definition.Description);
        if (current.Name == name && current.Description == description)
        {
            return CatalogResult<TalentSummary>.Success(current.ToSummary());
        }

        stream.AppendOne(new TalentDetailsChanged(talentId, name, normalizedName, description, timeProvider.GetUtcNow()));
        return await SaveAsync(session, talentId, cancellationToken);
    }

    public Task<CatalogResult<TalentSummary>> ArchiveTalentAsync(Guid talentId, CancellationToken cancellationToken = default) =>
        SetArchivedAsync(talentId, archived: true, cancellationToken);

    public Task<CatalogResult<TalentSummary>> UnarchiveTalentAsync(Guid talentId, CancellationToken cancellationToken = default) =>
        SetArchivedAsync(talentId, archived: false, cancellationToken);

    private async Task<CatalogResult<TalentSummary>> SetArchivedAsync(Guid talentId, bool archived, CancellationToken cancellationToken)
    {
        await using var session = sessions.OpenSession();
        var stream = await session.Events.FetchForWriting<TalentDocument>(talentId, cancellationToken);
        if (stream.Aggregate is not { } current)
        {
            return CatalogResult<TalentSummary>.Failure(CatalogOutcome.NotFound);
        }

        if (current.IsArchived == archived)
        {
            return CatalogResult<TalentSummary>.Success(current.ToSummary());
        }

        var now = timeProvider.GetUtcNow();
        stream.AppendOne(archived ? new TalentArchived(talentId, now) : new TalentUnarchived(talentId, now));
        return await SaveAsync(session, talentId, cancellationToken);
    }

    private static async Task<(CatalogOutcome Outcome, IEventStream<TalentDocument>? Stream)> FetchExpectedAsync(
        IDocumentSession session,
        Guid talentId,
        long expectedVersion,
        CancellationToken cancellationToken)
    {
        try
        {
            var stream = await session.Events.FetchForWriting<TalentDocument>(talentId, expectedVersion, cancellationToken);
            return stream.Aggregate is null ? (CatalogOutcome.NotFound, null) : (CatalogOutcome.Succeeded, stream);
        }
        catch (ConcurrencyException)
        {
            return await session.LoadAsync<TalentDocument>(talentId, cancellationToken) is null
                ? (CatalogOutcome.NotFound, null)
                : (CatalogOutcome.StaleVersion, null);
        }
    }

    private static Task<bool> IsNameUsedAsync(
        IQuerySession session,
        string normalizedName,
        Guid? exceptTalentId,
        CancellationToken cancellationToken)
    {
        var sameName = session.Query<TalentDocument>().Where(talent => talent.NormalizedName == normalizedName);
        return exceptTalentId is { } excluded
            ? sameName.Where(talent => talent.Id != excluded).AnyAsync(cancellationToken)
            : sameName.AnyAsync(cancellationToken);
    }

    private static async Task<CatalogResult<TalentSummary>> SaveAsync(IDocumentSession session, Guid talentId, CancellationToken cancellationToken)
    {
        try
        {
            if (!await session.TrySaveChangesAsync(cancellationToken))
            {
                return CatalogResult<TalentSummary>.Failure(CatalogOutcome.StaleVersion);
            }
        }
        catch (DocumentAlreadyExistsException)
        {
            // The unique index on NormalizedName rejects a concurrent command that claimed the same name.
            return CatalogResult<TalentSummary>.Failure(CatalogOutcome.NameAlreadyUsed);
        }

        var saved = await session.LoadAsync<TalentDocument>(talentId, cancellationToken);
        return CatalogResult<TalentSummary>.Success(saved!.ToSummary());
    }
}
