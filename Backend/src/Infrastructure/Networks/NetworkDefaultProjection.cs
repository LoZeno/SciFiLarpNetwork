using JasperFx.Events;
using Marten.Events.Aggregation;
using SciFiLarpNetwork.Backend.Domain.Events.NetworkEvents;
using SciFiLarpNetwork.Backend.Infrastructure.Networks.NetworkDocuments;

namespace SciFiLarpNetwork.Backend.Infrastructure.Networks;

public partial class NetworkDefaultProjection : SingleStreamProjection<NetworkDefaultDocument, Guid>
{
    public NetworkDefaultProjection()
    {
        IncludeType<DefaultNetworkChosen>();
    }

    public override NetworkDefaultDocument? Evolve(NetworkDefaultDocument? snapshot, Guid id, IEvent e) =>
        e.Data switch
        {
            DefaultNetworkChosen chosen => new NetworkDefaultDocument(id, chosen.NetworkId),
            _ => snapshot
        };
}
