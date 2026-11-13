using JasperFx.Events;
using Marten.Events.Aggregation;
using SciFiLarpNetwork.Backend.Domain.Events.AdministratorEvents;
using SciFiLarpNetwork.Backend.Infrastructure.Administration.AdministratorDocuments;

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
                // A consumed grant can never be used again. Returning null deletes the
                // projected document (Marten "Aggregation with Explicit Code" docs).
                return null;

            default:
                return snapshot;
        }
    }
}
