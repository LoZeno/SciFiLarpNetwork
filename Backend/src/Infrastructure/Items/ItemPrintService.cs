using JasperFx.Events;
using Marten;
using SciFiLarpNetwork.Backend.Application.Items;
using SciFiLarpNetwork.Backend.Domain.Events.ItemCopyEvents;
using SciFiLarpNetwork.Backend.Infrastructure.Items.ItemDocuments;
using SciFiLarpNetwork.Backend.Infrastructure.Persistence.EventStore;
using ExistingStreamIdCollisionException = Marten.Exceptions.ExistingStreamIdCollisionException;

namespace SciFiLarpNetwork.Backend.Infrastructure.Items;

/// <summary>
/// A print is its own stream, started with the identifier the client chose, so a repeated print request collides with
/// the print already made. Each copy is a new stream. The printed items are read but never appended to, so printing
/// never changes an item; their streams are locked so an item archived meanwhile rejects the print.
/// </summary>
internal sealed class ItemPrintService(AttributedSessionFactory sessions, TimeProvider timeProvider) : IItemPrintService
{
    public async Task<ItemResult<ItemCardSet>> PrintAsync(Guid printId, IReadOnlyList<Guid> itemIds, int copiesPerItem,
        CancellationToken cancellationToken = default)
    {
        if (printId == Guid.Empty)
        {
            return Failure(ItemOutcome.InvalidPrintId);
        }

        if (CheckRequest(itemIds, copiesPerItem) is var invalid and not ItemOutcome.Succeeded)
        {
            return Failure(invalid);
        }

        await using var session = sessions.OpenSession();
        if (await session.LoadAsync<ItemPrintDocument>(printId, cancellationToken) is { } existing)
        {
            return await RepeatedAsync(session, existing, itemIds, copiesPerItem, cancellationToken);
        }

        var (outcome, items) = await LockPrintableItemsAsync(session, itemIds, cancellationToken);
        if (outcome.Outcome != ItemOutcome.Succeeded)
        {
            return outcome;
        }

        var now = timeProvider.GetUtcNow();
        var copies = itemIds
            .SelectMany(itemId => Enumerable.Range(0, copiesPerItem).Select(_ => (ItemId: itemId, CopyId: Guid.NewGuid())))
            .ToArray();
        session.Events.StartStream<ItemPrintDocument>(printId,
            new ItemPrintCreated(printId, [.. itemIds], copiesPerItem, [.. copies.Select(copy => copy.CopyId)], now));
        foreach (var (itemId, copyId) in copies)
        {
            session.Events.StartStream<ItemCopyDocument>(copyId, new ItemCopyPrinted(copyId, itemId, printId, now));
        }

        try
        {
            if (!await session.TrySaveChangesAsync(cancellationToken))
            {
                return await AfterLostPrintAsync(printId, itemIds, copiesPerItem, ItemOutcome.ConcurrentChange, cancellationToken);
            }
        }
        catch (ExistingStreamIdCollisionException)
        {
            return await AfterLostPrintAsync(printId, itemIds, copiesPerItem, ItemOutcome.PrintIdInUse, cancellationToken);
        }

        var types = await TypeNamesAsync(session, items.Values, CancellationToken.None);
        return Success([.. copies.Select(copy => ToCard(items[copy.ItemId], types, ItemCopyCodes.Format(copy.CopyId)))]);
    }

    public async Task<ItemResult<ItemCardSet>> ReprintAsync(IReadOnlyList<Guid> copyIds, CancellationToken cancellationToken = default)
    {
        if (copyIds.Count == 0)
        {
            return Failure(ItemOutcome.NoCopiesSelected);
        }

        if (copyIds.Count > IItemPrintService.MaxCards)
        {
            return Failure(ItemOutcome.TooManyCards);
        }

        await using var query = sessions.QuerySession();
        var copies = (await query.LoadManyAsync<ItemCopyDocument>(cancellationToken, [.. copyIds.Distinct()]))
            .ToDictionary(copy => copy.Id);
        Guid[] unknown = [.. copyIds.Distinct().Where(id => !copies.ContainsKey(id))];
        if (unknown.Length > 0)
        {
            return ItemResult<ItemCardSet>.Unknown(ItemOutcome.UnknownCopies, unknown);
        }

        return await CardsAsync(query, [.. copyIds.Select(id => (copies[id].ItemId, ItemCopyCodes.Format(id)))], cancellationToken);
    }

    public async Task<ItemResult<ItemCardSet>> PreviewAsync(IReadOnlyList<Guid> itemIds, int copiesPerItem,
        CancellationToken cancellationToken = default)
    {
        if (CheckRequest(itemIds, copiesPerItem) is var invalid and not ItemOutcome.Succeeded)
        {
            return Failure(invalid);
        }

        await using var query = sessions.QuerySession();
        var items = (await query.LoadManyAsync<ItemDocument>(cancellationToken, [.. itemIds.Distinct()])).ToDictionary(item => item.Id);
        if (Unprintable(itemIds, items) is { } refused)
        {
            return refused;
        }

        return await CardsAsync(
            query,
            [.. itemIds.SelectMany(itemId => Enumerable.Repeat((itemId, IItemPrintService.SampleQrCode), copiesPerItem))],
            cancellationToken);
    }

    private static ItemOutcome CheckRequest(IReadOnlyList<Guid> itemIds, int copiesPerItem) =>
        itemIds.Count == 0 ? ItemOutcome.NoItemsSelected
        : copiesPerItem is < 1 or > IItemPrintService.MaxCopiesPerItem ? ItemOutcome.InvalidCopyCount
        : (long)itemIds.Count * copiesPerItem > IItemPrintService.MaxCards ? ItemOutcome.TooManyCards
        : ItemOutcome.Succeeded;

    private static ItemResult<ItemCardSet>? Unprintable(IReadOnlyList<Guid> itemIds, IReadOnlyDictionary<Guid, ItemDocument> items)
    {
        Guid[] unknown = [.. itemIds.Distinct().Where(id => !items.ContainsKey(id))];
        return unknown.Length > 0 ? ItemResult<ItemCardSet>.Unknown(ItemOutcome.UnknownItems, unknown)
            : items.Values.Any(item => item.IsArchived) ? Failure(ItemOutcome.ItemArchived)
            : null;
    }

    private static async Task<(ItemResult<ItemCardSet> Outcome, IReadOnlyDictionary<Guid, ItemDocument> Items)> LockPrintableItemsAsync(
        IDocumentSession session,
        IReadOnlyList<Guid> itemIds,
        CancellationToken cancellationToken)
    {
        Guid[] distinct = [.. itemIds.Distinct()];
        var streams = await session.Events.FetchManyForWriting<ItemDocument>(distinct, cancellationToken);
        var items = new Dictionary<Guid, ItemDocument>();
        foreach (var stream in streams)
        {
            stream.AlwaysEnforceConsistency = true;
            if (stream.Aggregate is { } item)
            {
                items[item.Id] = item;
            }
        }

        return (Unprintable(itemIds, items) ?? Success([]), items);
    }

    /// <summary>
    /// A repeated print request with the same items and count answers with the copies already made; any other content
    /// under the same identifier is refused.
    /// </summary>
    private static async Task<ItemResult<ItemCardSet>> RepeatedAsync(IQuerySession session, ItemPrintDocument print,
        IReadOnlyList<Guid> itemIds, int copiesPerItem, CancellationToken cancellationToken)
    {
        if (!print.ItemIds.SequenceEqual(itemIds) || print.CopiesPerItem != copiesPerItem)
        {
            return Failure(ItemOutcome.PrintIdInUse);
        }

        var copies = (await session.LoadManyAsync<ItemCopyDocument>(cancellationToken, print.CopyIds)).ToDictionary(copy => copy.Id);
        return await CardsAsync(session, [.. print.CopyIds.Select(id => (copies[id].ItemId, ItemCopyCodes.Format(id)))],
            cancellationToken);
    }

    private async Task<ItemResult<ItemCardSet>> AfterLostPrintAsync(Guid printId, IReadOnlyList<Guid> itemIds,
        int copiesPerItem, ItemOutcome otherwise, CancellationToken cancellationToken)
    {
        await using var query = sessions.QuerySession();
        return await query.LoadAsync<ItemPrintDocument>(printId, cancellationToken) is { } committed
            ? await RepeatedAsync(query, committed, itemIds, copiesPerItem, cancellationToken)
            : Failure(otherwise);
    }

    /// <summary>Builds the cards of <paramref name="cards"/> from the items' current details, in order.</summary>
    private static async Task<ItemResult<ItemCardSet>> CardsAsync(IQuerySession session,
        IReadOnlyList<(Guid ItemId, string QrCode)> cards, CancellationToken cancellationToken)
    {
        var items = (await session.LoadManyAsync<ItemDocument>(cancellationToken, [.. cards.Select(card => card.ItemId).Distinct()]))
            .ToDictionary(item => item.Id);
        var types = await TypeNamesAsync(session, items.Values, cancellationToken);
        return Success([.. cards.Select(card => ToCard(items[card.ItemId], types, card.QrCode))]);
    }

    private static async Task<IReadOnlyDictionary<Guid, string>> TypeNamesAsync(IQuerySession session,
        IEnumerable<ItemDocument> items, CancellationToken cancellationToken)
    {
        Guid[] typeIds = [.. items.Select(item => item.ItemTypeId).Distinct()];
        return typeIds.Length == 0
            ? new Dictionary<Guid, string>()
            : (await session.LoadManyAsync<ItemTypeDocument>(cancellationToken, typeIds)).ToDictionary(type => type.Id, type => type.Name);
    }

    private static ItemCard ToCard(ItemDocument item, IReadOnlyDictionary<Guid, string> types, string qrCode) =>
        new(
            types.GetValueOrDefault(item.ItemTypeId, string.Empty),
            item.Name,
            qrCode,
            item.Description,
            item.Effect,
            item.Charges,
            item.ExpiresOn,
            item.IsArchived);

    private static ItemResult<ItemCardSet> Success(IReadOnlyList<ItemCard> cards) => ItemResult<ItemCardSet>.Success(new ItemCardSet(cards));

    private static ItemResult<ItemCardSet> Failure(ItemOutcome outcome) => ItemResult<ItemCardSet>.Failure(outcome);
}
