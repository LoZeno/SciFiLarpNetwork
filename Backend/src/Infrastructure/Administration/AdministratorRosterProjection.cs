using JasperFx.Events;
using Marten.Events.Aggregation;
using SciFiLarpNetwork.Backend.Domain.Events.AdministratorEvents;
using SciFiLarpNetwork.Backend.Infrastructure.Administration.AdministratorDocuments;

namespace SciFiLarpNetwork.Backend.Infrastructure.Administration;

public partial class AdministratorRosterProjection : SingleStreamProjection<AdministratorRosterDocument, Guid>
{
    public AdministratorRosterProjection()
    {
        IncludeType<AdministratorRosterChanged>();
    }

    public override AdministratorRosterDocument? Evolve(AdministratorRosterDocument? snapshot, Guid id, IEvent e) =>
        e.Data switch
        {
            AdministratorRosterChanged changed => new AdministratorRosterDocument(id, changed.TargetAccountId),
            _ => snapshot
        };
}
