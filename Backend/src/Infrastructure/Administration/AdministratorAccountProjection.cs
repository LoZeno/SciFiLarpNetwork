using JasperFx.Events;
using Marten;
using Marten.Events;
using Marten.Events.Aggregation;
using SciFiLarpNetwork.Backend.Domain.Events;
using SciFiLarpNetwork.Backend.Infrastructure.Persistence.EventStore;

namespace SciFiLarpNetwork.Backend.Infrastructure.Administration;

public partial class AdministratorAccountProjection : SingleStreamProjection<AdministratorAccountDocument, Guid>
{
    public AdministratorAccountProjection()
    {
        Name = "AdministratorAccounts";
    }

    public override AdministratorAccountDocument? Evolve(AdministratorAccountDocument? snapshot, Guid id, IEvent e)
    {
        var persisted = (PersistedDomainEvent)e.Data;
        var payload = persisted.Payload;

        return persisted.EventType switch
        {
            AdministratorEvents.AccountCreated => new AdministratorAccountDocument(
                id,
                payload.GetProperty("userName").GetString() ?? string.Empty,
                payload.GetProperty("normalizedUserName").GetString() ?? string.Empty,
                payload.GetProperty("passwordHash").GetString() ?? string.Empty,
                payload.GetProperty("role").GetString() ?? string.Empty,
                payload.GetProperty("createdAtUtc").GetDateTimeOffset()),
            AdministratorEvents.AccountPasswordChanged when snapshot is not null =>
                snapshot with { PasswordHash = payload.GetProperty("passwordHash").GetString() ?? string.Empty },
            AdministratorEvents.AccountRoleChanged when snapshot is not null =>
                snapshot with { Role = payload.GetProperty("role").GetString() ?? string.Empty },
            _ => snapshot
        };
    }
}
