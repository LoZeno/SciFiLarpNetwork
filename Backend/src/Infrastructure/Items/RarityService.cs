using JasperFx;
using Marten;
using SciFiLarpNetwork.Backend.Application.Items;
using SciFiLarpNetwork.Backend.Domain.Events.RarityEvents;
using SciFiLarpNetwork.Backend.Domain.Items;
using SciFiLarpNetwork.Backend.Domain.Skills;
using SciFiLarpNetwork.Backend.Infrastructure.Items.ItemDocuments;
using SciFiLarpNetwork.Backend.Infrastructure.Persistence.EventStore;
using ExistingStreamIdCollisionException = Marten.Exceptions.ExistingStreamIdCollisionException;

namespace SciFiLarpNetwork.Backend.Infrastructure.Items;

/// <summary>
/// The order of all rarities is one stream: creating a rarity appends it there, and moving one rewrites the order, so
/// concurrent changes to the order conflict on that stream's version.
/// </summary>
internal sealed class RarityService(AttributedSessionFactory sessions, TimeProvider timeProvider) : IRarityService
{
    public async Task<IReadOnlyList<RaritySummary>> GetRaritiesAsync(bool includeArchived,
        CancellationToken cancellationToken = default)
    {
        await using var query = sessions.QuerySession();
        IQueryable<RarityDocument> rarities = query.Query<RarityDocument>();
        if (!includeArchived)
        {
            rarities = rarities.Where(rarity => !rarity.IsArchived);
        }

        var order = await OrderAsync(query, cancellationToken);
        return (await rarities.ToListAsync(cancellationToken))
            .Select(rarity => ToSummary(rarity, order))
            .OrderBy(rarity => rarity.Position)
            .ToArray();
    }

    public async Task<RaritySummary?> GetRarityAsync(Guid rarityId, CancellationToken cancellationToken = default)
    {
        await using var query = sessions.QuerySession();
        return await query.LoadAsync<RarityDocument>(rarityId, cancellationToken) is { } rarity
            ? ToSummary(rarity, await OrderAsync(query, cancellationToken))
            : null;
    }

    public async Task<ItemResult<RaritySummary>> CreateRarityAsync(string name, CancellationToken cancellationToken = default)
    {
        if (!CatalogNames.IsValid(name))
        {
            return ItemResult<RaritySummary>.Failure(ItemOutcome.InvalidName);
        }

        await using var session = sessions.OpenSession();
        var normalizedName = CatalogNames.Normalize(name);
        if (await IsNameUsedAsync(session, normalizedName, exceptId: null, cancellationToken))
        {
            return ItemResult<RaritySummary>.Failure(ItemOutcome.NameAlreadyUsed);
        }

        var rarityId = Guid.NewGuid();
        var now = timeProvider.GetUtcNow();
        var order = await session.Events.FetchForWriting<RarityOrderDocument>(RarityOrderDocument.StreamId, cancellationToken);
        session.Events.StartStream<RarityDocument>(rarityId, new RarityCreated(rarityId, name.Trim(), normalizedName, now));
        order.AppendOne(new RarityOrderChanged([.. order.Aggregate?.RarityIds ?? [], rarityId], now));
        return await SaveAsync(session, rarityId, ItemOutcome.ConcurrentChange, cancellationToken);
    }

    public async Task<ItemResult<RaritySummary>> RenameRarityAsync(Guid rarityId, long expectedVersion, string name,
        CancellationToken cancellationToken = default)
    {
        if (!CatalogNames.IsValid(name))
        {
            return ItemResult<RaritySummary>.Failure(ItemOutcome.InvalidName);
        }

        await using var session = sessions.OpenSession();
        var (outcome, stream) = await session.FetchExpectedAsync<RarityDocument>(rarityId, expectedVersion, cancellationToken);
        if (stream?.Aggregate is not { } current)
        {
            return ItemResult<RaritySummary>.Failure(outcome);
        }

        var normalizedName = CatalogNames.Normalize(name);
        if (await IsNameUsedAsync(session, normalizedName, rarityId, cancellationToken))
        {
            return ItemResult<RaritySummary>.Failure(ItemOutcome.NameAlreadyUsed);
        }

        if (current.Name == name.Trim())
        {
            return ItemResult<RaritySummary>.Success(ToSummary(current, await OrderAsync(session, cancellationToken)));
        }

        stream.AppendOne(new RarityRenamed(rarityId, name.Trim(), normalizedName, timeProvider.GetUtcNow()));
        return await SaveAsync(session, rarityId, ItemOutcome.StaleVersion, cancellationToken);
    }

    public async Task<ItemResult<RaritySummary>> MoveRarityAsync(Guid rarityId, bool up, CancellationToken cancellationToken = default)
    {
        await using var session = sessions.OpenSession();
        if (await session.LoadAsync<RarityDocument>(rarityId, cancellationToken) is not { } rarity)
        {
            return ItemResult<RaritySummary>.Failure(ItemOutcome.NotFound);
        }

        var order = await session.Events.FetchForWriting<RarityOrderDocument>(RarityOrderDocument.StreamId, cancellationToken);
        var current = order.Aggregate?.RarityIds ?? [];
        var moved = RarityOrder.Move(current, rarityId, up);
        if (moved.SequenceEqual(current))
        {
            return ItemResult<RaritySummary>.Success(ToSummary(rarity, [.. current]));
        }

        order.AppendOne(new RarityOrderChanged([.. moved], timeProvider.GetUtcNow()));
        return await SaveAsync(session, rarityId, ItemOutcome.ConcurrentChange, cancellationToken);
    }

    public Task<ItemResult<RaritySummary>> ArchiveRarityAsync(Guid rarityId, CancellationToken cancellationToken = default) =>
        SetArchivedAsync(rarityId, archived: true, cancellationToken);

    public Task<ItemResult<RaritySummary>> UnarchiveRarityAsync(Guid rarityId, CancellationToken cancellationToken = default) =>
        SetArchivedAsync(rarityId, archived: false, cancellationToken);

    private async Task<ItemResult<RaritySummary>> SetArchivedAsync(Guid rarityId, bool archived, CancellationToken cancellationToken)
    {
        await using var session = sessions.OpenSession();
        var stream = await session.Events.FetchForWriting<RarityDocument>(rarityId, cancellationToken);
        if (stream.Aggregate is not { } current)
        {
            return ItemResult<RaritySummary>.Failure(ItemOutcome.NotFound);
        }

        if (current.IsArchived == archived)
        {
            return ItemResult<RaritySummary>.Success(ToSummary(current, await OrderAsync(session, cancellationToken)));
        }

        var now = timeProvider.GetUtcNow();
        stream.AppendOne(archived ? new RarityArchived(rarityId, now) : new RarityUnarchived(rarityId, now));
        return await SaveAsync(session, rarityId, ItemOutcome.StaleVersion, cancellationToken);
    }

    private static async Task<Guid[]> OrderAsync(IQuerySession session, CancellationToken cancellationToken) =>
        (await session.LoadAsync<RarityOrderDocument>(RarityOrderDocument.StreamId, cancellationToken))?.RarityIds ?? [];

    private static Task<bool> IsNameUsedAsync(IQuerySession session, string normalizedName, Guid? exceptId,
        CancellationToken cancellationToken)
    {
        var sameName = session.Query<RarityDocument>().Where(rarity => rarity.NormalizedName == normalizedName);
        return exceptId is { } excluded
            ? sameName.Where(rarity => rarity.Id != excluded).AnyAsync(cancellationToken)
            : sameName.AnyAsync(cancellationToken);
    }

    /// <param name="lostRace">The outcome when a version check rejects the commit.</param>
    private static async Task<ItemResult<RaritySummary>> SaveAsync(IDocumentSession session, Guid rarityId,
        ItemOutcome lostRace, CancellationToken cancellationToken)
    {
        try
        {
            if (!await session.TrySaveChangesAsync(cancellationToken))
            {
                return ItemResult<RaritySummary>.Failure(lostRace);
            }
        }
        catch (ExistingStreamIdCollisionException)
        {
            // Two first-ever rarities both started the rarity-order stream; the later one lost.
            return ItemResult<RaritySummary>.Failure(ItemOutcome.ConcurrentChange);
        }
        catch (DocumentAlreadyExistsException)
        {
            // The unique index on NormalizedName rejects a concurrent command that claimed the same name.
            return ItemResult<RaritySummary>.Failure(ItemOutcome.NameAlreadyUsed);
        }

        // Once committed, a cancelled read-back must not look like a failed command.
        var saved = await session.LoadAsync<RarityDocument>(rarityId, CancellationToken.None);
        return ItemResult<RaritySummary>.Success(ToSummary(saved!, await OrderAsync(session, CancellationToken.None)));
    }

    /// <summary>A rarity missing from the order (never expected) is listed last.</summary>
    private static RaritySummary ToSummary(RarityDocument rarity, Guid[] order)
    {
        var position = Array.IndexOf(order, rarity.Id);
        return new RaritySummary(rarity.Id, rarity.Name, position < 0 ? order.Length : position, rarity.IsArchived, rarity.Version);
    }
}
