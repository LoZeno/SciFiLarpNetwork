using JasperFx.Events;
using Marten;
using Marten.Events;
using Marten.Events.Aggregation;
using SciFiLarpNetwork.Backend.Domain.Events;
using SciFiLarpNetwork.Backend.Infrastructure.Persistence.EventStore;

namespace SciFiLarpNetwork.Backend.Infrastructure.Administration;

public partial class AdministratorSessionProjection : SingleStreamProjection<AdministratorSessionDocument, Guid>
{
    public AdministratorSessionProjection()
    {
        Name = "AdministratorSessions";
    }

    public override AdministratorSessionDocument? Evolve(AdministratorSessionDocument? snapshot, Guid id, IEvent e)
    {
        var persisted = (PersistedDomainEvent)e.Data;
        var payload = persisted.Payload;

        return persisted.EventType switch
        {
            AdministratorEvents.SessionCreated => new AdministratorSessionDocument(
                id,
                payload.GetProperty("tokenHash").GetString() ?? string.Empty,
                payload.GetProperty("accountId").GetGuid(),
                payload.GetProperty("expiresAtUtc").GetDateTimeOffset()),
            AdministratorEvents.SessionRevoked => null,
            _ => snapshot
        };
    }
}
