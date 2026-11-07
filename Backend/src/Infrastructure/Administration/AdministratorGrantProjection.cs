using JasperFx.Events;
using Marten;
using Marten.Events;
using Marten.Events.Aggregation;
using SciFiLarpNetwork.Backend.Domain.Events;
using SciFiLarpNetwork.Backend.Infrastructure.Persistence.EventStore;

namespace SciFiLarpNetwork.Backend.Infrastructure.Administration;

public partial class AdministratorGrantProjection : SingleStreamProjection<AdministratorGrantDocument, Guid>
{
    public AdministratorGrantProjection()
    {
        Name = "AdministratorGrants";
    }

    public override AdministratorGrantDocument? Evolve(AdministratorGrantDocument? snapshot, Guid id, IEvent e)
    {
        var persisted = (PersistedDomainEvent)e.Data;
        var payload = persisted.Payload;

        return persisted.EventType switch
        {
            AdministratorEvents.BootstrapCredentialIssued or AdministratorEvents.RecoveryCredentialIssued =>
                new AdministratorGrantDocument(
                    id,
                    payload.GetProperty("purpose").GetString() ?? string.Empty,
                    payload.GetProperty("tokenHash").GetString() ?? string.Empty,
                    payload.GetProperty("expiresAtUtc").GetDateTimeOffset()),
            AdministratorEvents.GrantConsumed => snapshot,
            _ => snapshot
        };
    }
}
