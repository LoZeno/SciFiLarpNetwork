using JasperFx.Events;
using Marten.Events.Aggregation;
using SciFiLarpNetwork.Backend.Domain.Events.ItemCopyEvents;
using SciFiLarpNetwork.Backend.Infrastructure.Items.ItemDocuments;

namespace SciFiLarpNetwork.Backend.Infrastructure.Items;

public partial class ItemCopyProjection : SingleStreamProjection<ItemCopyDocument, Guid>
{
    public ItemCopyProjection()
    {
        IncludeType<ItemCopyPrinted>();
        IncludeType<ItemCopyDeactivated>();
        IncludeType<ItemCopyReactivated>();
    }

    public override ItemCopyDocument? Evolve(ItemCopyDocument? snapshot, Guid id, IEvent e) =>
        e.Data switch
        {
            ItemCopyPrinted printed => new ItemCopyDocument(
                printed.CopyId,
                printed.ItemId,
                printed.PrintId,
                printed.OccurredAtUtc,
                e.UserName,
                IsActive: true,
                ChangedAtUtc: null,
                ChangedBy: null),
            ItemCopyDeactivated deactivated when snapshot is not null => snapshot with
            {
                IsActive = false,
                ChangedAtUtc = deactivated.OccurredAtUtc,
                ChangedBy = e.UserName
            },
            ItemCopyReactivated reactivated when snapshot is not null => snapshot with
            {
                IsActive = true,
                ChangedAtUtc = reactivated.OccurredAtUtc,
                ChangedBy = e.UserName
            },
            _ => snapshot
        };
}
