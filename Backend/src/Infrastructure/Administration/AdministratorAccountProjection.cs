using JasperFx.Events;
using Marten.Events.Aggregation;
using SciFiLarpNetwork.Backend.Domain.Events.AdministratorEvents;
using SciFiLarpNetwork.Backend.Infrastructure.Administration.AdministratorDocuments;

namespace SciFiLarpNetwork.Backend.Infrastructure.Administration;

public partial class AdministratorAccountProjection : SingleStreamProjection<AdministratorAccountDocument, Guid>
{
    public AdministratorAccountProjection()
    {
        IncludeType<AccountCreated>();
        IncludeType<AccountPasswordChanged>();
        IncludeType<AccountRoleChanged>();
    }

    public override AdministratorAccountDocument? Evolve(
        AdministratorAccountDocument? snapshot, Guid id, IEvent e)
    {
        switch (e.Data)
        {
            case AccountCreated created:
                return new AdministratorAccountDocument(
                    created.AccountId,
                    created.UserName,
                    created.NormalizedUserName,
                    created.PasswordHash,
                    created.Role,
                    created.OccurredAtUtc);

            case AccountPasswordChanged passwordChanged when snapshot is not null:
                return snapshot with { PasswordHash = passwordChanged.PasswordHash };

            case AccountRoleChanged roleChanged when snapshot is not null:
                return snapshot with { Role = roleChanged.NewRole };
        }

        return snapshot;
    }
}
