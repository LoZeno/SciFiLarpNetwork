using JasperFx;
using Marten;
using SciFiLarpNetwork.Backend.Application.Items;
using SciFiLarpNetwork.Backend.Domain.Events.ItemTypeEvents;
using SciFiLarpNetwork.Backend.Domain.Skills;
using SciFiLarpNetwork.Backend.Infrastructure.Items.ItemDocuments;
using SciFiLarpNetwork.Backend.Infrastructure.Persistence.EventStore;

namespace SciFiLarpNetwork.Backend.Infrastructure.Items;

internal sealed class ItemTypeService(AttributedSessionFactory sessions, TimeProvider timeProvider) : IItemTypeService
{
    public async Task<IReadOnlyList<ItemTypeSummary>> GetItemTypesAsync(bool includeArchived,
        CancellationToken cancellationToken = default)
    {
        await using var query = sessions.QuerySession();
        IQueryable<ItemTypeDocument> types = query.Query<ItemTypeDocument>();
        if (!includeArchived)
        {
            types = types.Where(type => !type.IsArchived);
        }

        return (await types.ToListAsync(cancellationToken))
            .OrderBy(type => type.NormalizedName, StringComparer.Ordinal)
            .Select(ToSummary)
            .ToArray();
    }

    public async Task<ItemTypeSummary?> GetItemTypeAsync(Guid itemTypeId, CancellationToken cancellationToken = default)
    {
        await using var query = sessions.QuerySession();
        return await query.LoadAsync<ItemTypeDocument>(itemTypeId, cancellationToken) is { } type ? ToSummary(type) : null;
    }

    public async Task<ItemResult<ItemTypeSummary>> CreateItemTypeAsync(string name, CancellationToken cancellationToken = default)
    {
        if (!CatalogNames.IsValid(name))
        {
            return ItemResult<ItemTypeSummary>.Failure(ItemOutcome.InvalidName);
        }

        await using var session = sessions.OpenSession();
        var normalizedName = CatalogNames.Normalize(name);
        if (await IsNameUsedAsync(session, normalizedName, exceptId: null, cancellationToken))
        {
            return ItemResult<ItemTypeSummary>.Failure(ItemOutcome.NameAlreadyUsed);
        }

        var itemTypeId = Guid.NewGuid();
        session.Events.StartStream<ItemTypeDocument>(itemTypeId,
            new ItemTypeCreated(itemTypeId, name.Trim(), normalizedName, timeProvider.GetUtcNow()));
        return await SaveAsync(session, itemTypeId, cancellationToken);
    }

    public async Task<ItemResult<ItemTypeSummary>> RenameItemTypeAsync(Guid itemTypeId, long expectedVersion, string name,
        CancellationToken cancellationToken = default)
    {
        if (!CatalogNames.IsValid(name))
        {
            return ItemResult<ItemTypeSummary>.Failure(ItemOutcome.InvalidName);
        }

        await using var session = sessions.OpenSession();
        var (outcome, stream) = await session.FetchExpectedAsync<ItemTypeDocument>(itemTypeId, expectedVersion, cancellationToken);
        if (stream?.Aggregate is not { } current)
        {
            return ItemResult<ItemTypeSummary>.Failure(outcome);
        }

        var normalizedName = CatalogNames.Normalize(name);
        if (await IsNameUsedAsync(session, normalizedName, itemTypeId, cancellationToken))
        {
            return ItemResult<ItemTypeSummary>.Failure(ItemOutcome.NameAlreadyUsed);
        }

        if (current.Name == name.Trim())
        {
            return ItemResult<ItemTypeSummary>.Success(ToSummary(current));
        }

        stream.AppendOne(new ItemTypeRenamed(itemTypeId, name.Trim(), normalizedName, timeProvider.GetUtcNow()));
        return await SaveAsync(session, itemTypeId, cancellationToken);
    }

    public Task<ItemResult<ItemTypeSummary>> ArchiveItemTypeAsync(Guid itemTypeId, CancellationToken cancellationToken = default) =>
        SetArchivedAsync(itemTypeId, archived: true, cancellationToken);

    public Task<ItemResult<ItemTypeSummary>> UnarchiveItemTypeAsync(Guid itemTypeId, CancellationToken cancellationToken = default) =>
        SetArchivedAsync(itemTypeId, archived: false, cancellationToken);

    private async Task<ItemResult<ItemTypeSummary>> SetArchivedAsync(Guid itemTypeId, bool archived,
        CancellationToken cancellationToken)
    {
        await using var session = sessions.OpenSession();
        var stream = await session.Events.FetchForWriting<ItemTypeDocument>(itemTypeId, cancellationToken);
        if (stream.Aggregate is not { } current)
        {
            return ItemResult<ItemTypeSummary>.Failure(ItemOutcome.NotFound);
        }

        if (current.IsArchived == archived)
        {
            return ItemResult<ItemTypeSummary>.Success(ToSummary(current));
        }

        var now = timeProvider.GetUtcNow();
        stream.AppendOne(archived ? new ItemTypeArchived(itemTypeId, now) : new ItemTypeUnarchived(itemTypeId, now));
        return await SaveAsync(session, itemTypeId, cancellationToken);
    }

    private static Task<bool> IsNameUsedAsync(IQuerySession session, string normalizedName, Guid? exceptId,
        CancellationToken cancellationToken)
    {
        var sameName = session.Query<ItemTypeDocument>().Where(type => type.NormalizedName == normalizedName);
        return exceptId is { } excluded
            ? sameName.Where(type => type.Id != excluded).AnyAsync(cancellationToken)
            : sameName.AnyAsync(cancellationToken);
    }

    private static async Task<ItemResult<ItemTypeSummary>> SaveAsync(IDocumentSession session, Guid itemTypeId,
        CancellationToken cancellationToken)
    {
        try
        {
            if (!await session.TrySaveChangesAsync(cancellationToken))
            {
                return ItemResult<ItemTypeSummary>.Failure(ItemOutcome.StaleVersion);
            }
        }
        catch (DocumentAlreadyExistsException)
        {
            // The unique index on NormalizedName rejects a concurrent command that claimed the same name.
            return ItemResult<ItemTypeSummary>.Failure(ItemOutcome.NameAlreadyUsed);
        }

        var saved = await session.LoadAsync<ItemTypeDocument>(itemTypeId, cancellationToken);
        return ItemResult<ItemTypeSummary>.Success(ToSummary(saved!));
    }

    private static ItemTypeSummary ToSummary(ItemTypeDocument type) => new(type.Id, type.Name, type.IsArchived, type.Version);
}
