using JasperFx.Events;
using Marten.Events.Aggregation;
using SciFiLarpNetwork.Backend.Domain.Events.ItemTypeEvents;
using SciFiLarpNetwork.Backend.Infrastructure.Items.ItemDocuments;

namespace SciFiLarpNetwork.Backend.Infrastructure.Items;

public partial class ItemTypeProjection : SingleStreamProjection<ItemTypeDocument, Guid>
{
    public ItemTypeProjection()
    {
        IncludeType<ItemTypeCreated>();
        IncludeType<ItemTypeRenamed>();
        IncludeType<ItemTypeArchived>();
        IncludeType<ItemTypeUnarchived>();
    }

    public override ItemTypeDocument? Evolve(ItemTypeDocument? snapshot, Guid id, IEvent e) =>
        e.Data switch
        {
            ItemTypeCreated created => new ItemTypeDocument(created.ItemTypeId, created.Name, created.NormalizedName, IsArchived: false),
            ItemTypeRenamed renamed when snapshot is not null => snapshot with { Name = renamed.Name, NormalizedName = renamed.NormalizedName },
            ItemTypeArchived when snapshot is not null => snapshot with { IsArchived = true },
            ItemTypeUnarchived when snapshot is not null => snapshot with { IsArchived = false },
            _ => snapshot
        };
}
