using Marten;
using SciFiLarpNetwork.Backend.Application.Players;
using SciFiLarpNetwork.Backend.Domain.Events.CharacterStatEvents;
using SciFiLarpNetwork.Backend.Domain.Skills;
using SciFiLarpNetwork.Backend.Infrastructure.Persistence.EventStore;
using SciFiLarpNetwork.Backend.Infrastructure.Players.PlayerDocuments;

namespace SciFiLarpNetwork.Backend.Infrastructure.Players;

internal sealed class CharacterStatService(AttributedSessionFactory sessions, TimeProvider timeProvider)
    : ICharacterStatService
{
    public async Task<IReadOnlyList<CharacterStatSummary>> GetStatsAsync(bool includeArchived,
        CancellationToken cancellationToken = default)
    {
        await using var session = sessions.QuerySession();
        var stats = includeArchived
            ? await session.Query<CharacterStatDocument>().ToListAsync(cancellationToken)
            : await session.Query<CharacterStatDocument>().Where(stat => !stat.IsArchived).ToListAsync(cancellationToken);
        return stats
            .OrderBy(stat => stat.NormalizedName, StringComparer.Ordinal)
            .Select(stat => stat.ToSummary())
            .ToArray();
    }

    public async Task<CharacterStatSummary?> GetStatAsync(Guid statId, CancellationToken cancellationToken = default)
    {
        await using var session = sessions.QuerySession();
        return (await session.LoadAsync<CharacterStatDocument>(statId, cancellationToken))?.ToSummary();
    }

    public async Task<RosterResult<CharacterStatSummary>> RenameStatAsync(
        Guid statId,
        long expectedVersion,
        string name,
        CancellationToken cancellationToken = default)
    {
        if (!CatalogNames.IsValid(name))
        {
            return RosterResult<CharacterStatSummary>.Failure(RosterOutcome.InvalidStatName);
        }

        await using var session = sessions.OpenSession();
        var (outcome, stream) =
            await session.FetchExpectedAsync<CharacterStatDocument>(statId, expectedVersion, cancellationToken);
        if (stream?.Aggregate is not { } current)
        {
            return RosterResult<CharacterStatSummary>.Failure(outcome);
        }

        var trimmed = name.Trim();
        if (trimmed == current.Name)
        {
            return RosterResult<CharacterStatSummary>.Success(current.ToSummary());
        }

        var normalizedName = CatalogNames.Normalize(name);
        if (await session.Query<CharacterStatDocument>()
                .AnyAsync(stat => stat.NormalizedName == normalizedName && stat.Id != statId, cancellationToken))
        {
            return RosterResult<CharacterStatSummary>.Failure(RosterOutcome.NameAlreadyUsed);
        }

        stream.AppendOne(new CharacterStatRenamed(statId, trimmed, normalizedName, timeProvider.GetUtcNow()));
        return await SaveAsync(session, statId, RosterOutcome.StaleVersion, cancellationToken);
    }

    public Task<RosterResult<CharacterStatSummary>> ArchiveStatAsync(Guid statId,
        CancellationToken cancellationToken = default) =>
        SetArchivedAsync(statId, archived: true, cancellationToken);

    public Task<RosterResult<CharacterStatSummary>> UnarchiveStatAsync(Guid statId,
        CancellationToken cancellationToken = default) =>
        SetArchivedAsync(statId, archived: false, cancellationToken);

    private async Task<RosterResult<CharacterStatSummary>> SetArchivedAsync(Guid statId, bool archived,
        CancellationToken cancellationToken)
    {
        await using var session = sessions.OpenSession();
        var stream = await session.Events.FetchForWriting<CharacterStatDocument>(statId, cancellationToken);
        if (stream.Aggregate is not { } current)
        {
            return RosterResult<CharacterStatSummary>.Failure(RosterOutcome.NotFound);
        }

        if (current.IsArchived == archived)
        {
            return RosterResult<CharacterStatSummary>.Success(current.ToSummary());
        }

        var now = timeProvider.GetUtcNow();
        stream.AppendOne(archived ? new CharacterStatArchived(statId, now) : new CharacterStatUnarchived(statId, now));
        return await SaveAsync(session, statId, RosterOutcome.ConcurrentChange, cancellationToken);
    }

    /// <param name="lostRace">
    /// <see cref="RosterOutcome.StaleVersion"/> for an edit based on a version, which another command overtook;
    /// otherwise <see cref="RosterOutcome.ConcurrentChange"/>.
    /// </param>
    private static async Task<RosterResult<CharacterStatSummary>> SaveAsync(
        IDocumentSession session,
        Guid statId,
        RosterOutcome lostRace,
        CancellationToken cancellationToken)
    {
        var outcome = await session.CommitAsync(
            () => Task.FromResult(lostRace),
            RosterWrites.NameAlreadyUsed,
            cancellationToken);
        if (outcome != RosterOutcome.Succeeded)
        {
            return RosterResult<CharacterStatSummary>.Failure(outcome);
        }

        var saved = await session.LoadAsync<CharacterStatDocument>(statId, cancellationToken);
        return RosterResult<CharacterStatSummary>.Success(saved!.ToSummary());
    }
}
