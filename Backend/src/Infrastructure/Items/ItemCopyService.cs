using Marten;
using SciFiLarpNetwork.Backend.Application.Items;
using SciFiLarpNetwork.Backend.Domain.Events.ItemCopyEvents;
using SciFiLarpNetwork.Backend.Infrastructure.Items.ItemDocuments;
using SciFiLarpNetwork.Backend.Infrastructure.News;
using SciFiLarpNetwork.Backend.Infrastructure.Persistence.EventStore;

namespace SciFiLarpNetwork.Backend.Infrastructure.Items;

/// <summary>
/// Each copy is its own stream, so deactivating one copy never conflicts with changes to its item or to other copies,
/// and two commands changing the same copy at once conflict on that copy's version.
/// </summary>
internal sealed class ItemCopyService(AttributedSessionFactory sessions, TimeProvider timeProvider) : IItemCopyService
{
    public async Task<IReadOnlyList<ItemCopySummary>?> GetCopiesAsync(Guid itemId, CancellationToken cancellationToken = default)
    {
        await using var query = sessions.QuerySession();
        if (await query.LoadAsync<ItemDocument>(itemId, cancellationToken) is null)
        {
            return null;
        }

        var copies = await query.Query<ItemCopyDocument>()
            .Where(copy => copy.ItemId == itemId)
            .ToListAsync(cancellationToken);
        var administrators = await NewsAdministrators.LoadAsync(
            query, copies.SelectMany(copy => new[] { copy.PrintedBy, copy.ChangedBy }), cancellationToken);
        return
        [
            .. copies
                .OrderByDescending(copy => copy.PrintedAtUtc)
                .ThenBy(copy => copy.PrintId)
                .ThenBy(copy => copy.Id)
                .Select(copy => ToSummary(copy, administrators))
        ];
    }

    public Task<ItemResult<ItemCopySummary>> DeactivateCopyAsync(Guid copyId, CancellationToken cancellationToken = default) =>
        SetActiveAsync(copyId, active: false, cancellationToken);

    public Task<ItemResult<ItemCopySummary>> ReactivateCopyAsync(Guid copyId, CancellationToken cancellationToken = default) =>
        SetActiveAsync(copyId, active: true, cancellationToken);

    private async Task<ItemResult<ItemCopySummary>> SetActiveAsync(Guid copyId, bool active, CancellationToken cancellationToken)
    {
        await using var session = sessions.OpenSession();
        var stream = await session.Events.FetchForWriting<ItemCopyDocument>(copyId, cancellationToken);
        if (stream.Aggregate is not { } current)
        {
            return ItemResult<ItemCopySummary>.Failure(ItemOutcome.NotFound);
        }

        if (current.IsActive != active)
        {
            var now = timeProvider.GetUtcNow();
            stream.AppendOne(active
                ? new ItemCopyReactivated(copyId, current.ItemId, now)
                : new ItemCopyDeactivated(copyId, current.ItemId, now));
            if (!await session.TrySaveChangesAsync(cancellationToken))
            {
                return ItemResult<ItemCopySummary>.Failure(ItemOutcome.ConcurrentChange);
            }
        }

        var saved = await session.LoadAsync<ItemCopyDocument>(copyId, CancellationToken.None);
        var administrators = await NewsAdministrators.LoadAsync(session, [saved!.PrintedBy, saved.ChangedBy], CancellationToken.None);
        return ItemResult<ItemCopySummary>.Success(ToSummary(saved, administrators));
    }

    private static ItemCopySummary ToSummary(ItemCopyDocument copy, NewsAdministrators administrators) =>
        new(
            copy.Id,
            ItemCopyCodes.Format(copy.Id),
            copy.ItemId,
            copy.PrintId,
            copy.PrintedAtUtc,
            administrators.Describe(copy.PrintedBy),
            copy.IsActive,
            copy.ChangedAtUtc,
            administrators.Describe(copy.ChangedBy));
}
