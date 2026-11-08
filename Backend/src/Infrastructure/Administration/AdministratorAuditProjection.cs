using JasperFx.Events;
using Marten.Events.Aggregation;
using SciFiLarpNetwork.Backend.Domain.Events;

namespace SciFiLarpNetwork.Backend.Infrastructure.Administration;

public partial class AdministratorAuditProjection : SingleStreamProjection<AdministratorAuditDocument, Guid>
{
    public AdministratorAuditProjection()
    {
        IncludeType<AuditEventLogged>();
    }

    public override AdministratorAuditDocument? Evolve(
        AdministratorAuditDocument? snapshot, Guid id, IEvent e)
    {
        switch (e.Data)
        {
            case AuditEventLogged logged:
                return new AdministratorAuditDocument(
                    logged.AuditId,
                    logged.ActorKind,
                    logged.ActorAccountId,
                    logged.TargetAccountId,
                    logged.EventType,
                    logged.PreviousRole,
                    logged.NewRole,
                    logged.OccurredAtUtc);

            default:
                return snapshot;
        }
    }
}
