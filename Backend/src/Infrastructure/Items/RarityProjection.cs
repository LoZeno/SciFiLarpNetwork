using JasperFx.Events;
using Marten.Events.Aggregation;
using SciFiLarpNetwork.Backend.Domain.Events.RarityEvents;
using SciFiLarpNetwork.Backend.Infrastructure.Items.ItemDocuments;

namespace SciFiLarpNetwork.Backend.Infrastructure.Items;

public partial class RarityProjection : SingleStreamProjection<RarityDocument, Guid>
{
    public RarityProjection()
    {
        IncludeType<RarityCreated>();
        IncludeType<RarityRenamed>();
        IncludeType<RarityArchived>();
        IncludeType<RarityUnarchived>();
    }

    public override RarityDocument? Evolve(RarityDocument? snapshot, Guid id, IEvent e) =>
        e.Data switch
        {
            RarityCreated created => new RarityDocument(created.RarityId, created.Name, created.NormalizedName, IsArchived: false),
            RarityRenamed renamed when snapshot is not null => snapshot with { Name = renamed.Name, NormalizedName = renamed.NormalizedName },
            RarityArchived when snapshot is not null => snapshot with { IsArchived = true },
            RarityUnarchived when snapshot is not null => snapshot with { IsArchived = false },
            _ => snapshot
        };
}
