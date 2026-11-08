using JasperFx.Events;
using Marten.Events.Aggregation;
using SciFiLarpNetwork.Backend.Domain.Events;

namespace SciFiLarpNetwork.Backend.Infrastructure.Administration;

public partial class AdministratorSessionProjection : SingleStreamProjection<AdministratorSessionDocument, Guid>
{
    public AdministratorSessionProjection()
    {
        IncludeType<SessionCreated>();
        IncludeType<SessionRevoked>();
    }

    public override AdministratorSessionDocument? Evolve(
        AdministratorSessionDocument? snapshot, Guid id, IEvent e)
    {
        switch (e.Data)
        {
            case SessionCreated created:
                return new AdministratorSessionDocument(
                    created.SessionId,
                    created.TokenHash,
                    created.AccountId,
                    created.ExpiresAtUtc);

            case SessionRevoked:
                return null;

            default:
                return snapshot;
        }
    }
}
