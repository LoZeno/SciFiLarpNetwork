using JasperFx.Events;
using Marten;
using Marten.Events;
using Marten.Events.Aggregation;
using SciFiLarpNetwork.Backend.Infrastructure.Persistence.EventStore;

namespace SciFiLarpNetwork.Backend.Infrastructure.Administration;

public partial class AdministratorAuditProjection : SingleStreamProjection<AdministratorAuditDocument, Guid>
{
    public AdministratorAuditProjection()
    {
        Name = "AdministratorAudit";
    }

    public override AdministratorAuditDocument? Evolve(AdministratorAuditDocument? snapshot, Guid id, IEvent e)
    {
        var persisted = (PersistedDomainEvent)e.Data;
        var payload = persisted.Payload;

        return new AdministratorAuditDocument(
            persisted.EventId,
            payload.GetProperty("actorKind").GetString() ?? string.Empty,
            payload.TryGetProperty("actorId", out var actorIdProp) && actorIdProp.ValueKind != System.Text.Json.JsonValueKind.Null
                ? (Guid?)actorIdProp.GetGuid()
                : null,
            payload.GetProperty("targetAccountId").GetGuid(),
            payload.GetProperty("eventType").GetString() ?? string.Empty,
            payload.GetProperty("previousRole").GetString() ?? string.Empty,
            payload.GetProperty("newRole").GetString() ?? string.Empty,
            payload.GetProperty("occurredAtUtc").GetDateTimeOffset());
    }
}
