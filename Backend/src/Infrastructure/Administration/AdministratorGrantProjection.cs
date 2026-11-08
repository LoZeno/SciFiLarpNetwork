using JasperFx.Events;
using Marten.Events.Aggregation;
using SciFiLarpNetwork.Backend.Domain.Events;

namespace SciFiLarpNetwork.Backend.Infrastructure.Administration;

public partial class AdministratorGrantProjection : SingleStreamProjection<AdministratorGrantDocument, Guid>
{
    public AdministratorGrantProjection()
    {
        IncludeType<BootstrapCredentialIssued>();
        IncludeType<RecoveryCredentialIssued>();
        IncludeType<GrantConsumed>();
    }

    public override AdministratorGrantDocument? Evolve(
        AdministratorGrantDocument? snapshot, Guid id, IEvent e)
    {
        switch (e.Data)
        {
            case BootstrapCredentialIssued bootstrap:
                return new AdministratorGrantDocument(
                    id,
                    "bootstrap",
                    bootstrap.TokenHash,
                    bootstrap.ExpiresAtUtc);

            case RecoveryCredentialIssued recovery:
                return new AdministratorGrantDocument(
                    id,
                    "recovery",
                    recovery.TokenHash,
                    recovery.ExpiresAtUtc);

            case GrantConsumed:
                // Grant remains but is logically consumed — keep the document
                return snapshot;

            default:
                return snapshot;
        }
    }
}
