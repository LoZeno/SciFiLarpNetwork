using JasperFx.Events;
using Marten.Events.Aggregation;
using SciFiLarpNetwork.Backend.Domain.Events.RarityEvents;
using SciFiLarpNetwork.Backend.Infrastructure.Items.ItemDocuments;

namespace SciFiLarpNetwork.Backend.Infrastructure.Items;

public partial class RarityOrderProjection : SingleStreamProjection<RarityOrderDocument, Guid>
{
    public RarityOrderProjection()
    {
        IncludeType<RarityOrderChanged>();
    }

    public override RarityOrderDocument? Evolve(RarityOrderDocument? snapshot, Guid id, IEvent e) =>
        e.Data switch
        {
            RarityOrderChanged changed => new RarityOrderDocument(id, [.. changed.RarityIds]),
            _ => snapshot
        };
}
