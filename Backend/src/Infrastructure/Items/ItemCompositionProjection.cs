using JasperFx.Events;
using Marten.Events.Aggregation;
using SciFiLarpNetwork.Backend.Domain.Events.ItemEvents;
using SciFiLarpNetwork.Backend.Infrastructure.Items.ItemDocuments;

namespace SciFiLarpNetwork.Backend.Infrastructure.Items;

public partial class ItemCompositionProjection : SingleStreamProjection<ItemCompositionDocument, Guid>
{
    public ItemCompositionProjection()
    {
        IncludeType<ItemCompositionChanged>();
    }

    public override ItemCompositionDocument? Evolve(ItemCompositionDocument? snapshot, Guid id, IEvent e) =>
        e.Data switch
        {
            ItemCompositionChanged changed => new ItemCompositionDocument(id, changed.ItemId),
            _ => snapshot
        };
}
