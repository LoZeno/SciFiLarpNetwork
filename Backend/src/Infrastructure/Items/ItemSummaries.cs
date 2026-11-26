using Marten;
using SciFiLarpNetwork.Backend.Application.Items;
using SciFiLarpNetwork.Backend.Infrastructure.Items.ItemDocuments;

namespace SciFiLarpNetwork.Backend.Infrastructure.Items;

/// <summary>Turns item documents into summaries, with the current names of their components.</summary>
internal static class ItemSummaries
{
    extension(IQuerySession session)
    {
        public async Task<ItemSummary> SummarizeAsync(ItemDocument item, CancellationToken cancellationToken) =>
            (await session.SummarizeAsync([item], cancellationToken))[0];

        public async Task<IReadOnlyList<ItemSummary>> SummarizeAsync(IReadOnlyList<ItemDocument> items,
            CancellationToken cancellationToken)
        {
            Guid[] componentIds = [.. items.SelectMany(item => item.Components).Select(component => component.ItemId).Distinct()];
            var components = componentIds.Length == 0
                ? new Dictionary<Guid, ItemDocument>()
                : (await session.LoadManyAsync<ItemDocument>(cancellationToken, componentIds)).ToDictionary(item => item.Id);
            return [.. items.Select(item => ToSummary(item, components))];
        }
    }

    private static ItemSummary ToSummary(ItemDocument item, IReadOnlyDictionary<Guid, ItemDocument> components) =>
        new(
            item.Id,
            item.Name,
            item.ItemTypeId,
            item.RarityId,
            item.Description,
            item.Effect,
            item.Cost,
            item.Charges,
            item.ExpiresOn,
            item.ImageId,
            [
                .. item.Components
                    .Select(component => components.TryGetValue(component.ItemId, out var known)
                        ? new ItemComponentSummary(component.ItemId, known.Name, component.Quantity, known.IsArchived)
                        : new ItemComponentSummary(component.ItemId, string.Empty, component.Quantity, IsArchived: false))
                    .OrderBy(component => component.Name, StringComparer.CurrentCultureIgnoreCase)
            ],
            item.IsArchived,
            item.Version);
}
