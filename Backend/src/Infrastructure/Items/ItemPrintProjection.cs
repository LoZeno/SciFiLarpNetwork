using JasperFx.Events;
using Marten.Events.Aggregation;
using SciFiLarpNetwork.Backend.Domain.Events.ItemCopyEvents;
using SciFiLarpNetwork.Backend.Infrastructure.Items.ItemDocuments;

namespace SciFiLarpNetwork.Backend.Infrastructure.Items;

public partial class ItemPrintProjection : SingleStreamProjection<ItemPrintDocument, Guid>
{
    public ItemPrintProjection()
    {
        IncludeType<ItemPrintCreated>();
    }

    public override ItemPrintDocument? Evolve(ItemPrintDocument? snapshot, Guid id, IEvent e) =>
        e.Data switch
        {
            ItemPrintCreated created => new ItemPrintDocument(
                created.PrintId, [.. created.ItemIds], created.CopiesPerItem, [.. created.CopyIds]),
            _ => snapshot
        };
}
