using Marten;
using SciFiLarpNetwork.Backend.Domain.Events.AdministratorEvents;
using SciFiLarpNetwork.Backend.Infrastructure.Administration.AdministratorDocuments;

namespace SciFiLarpNetwork.Backend.Infrastructure.Administration;

internal static class AdministratorDocumentSessionExtensions
{
    extension(IDocumentSession session)
    {
        public void StartAuditStream(
            string actorKind,
            Guid? actorAccountId,
            Guid targetAccountId,
            string eventType,
            string previousRole,
            string newRole,
            DateTimeOffset occurredAtUtc)
        {
            var auditId = Guid.NewGuid();
            session.Events.StartStream<AdministratorAuditDocument>(
                auditId,
                new AuditEventLogged(auditId, actorKind, actorAccountId, targetAccountId, eventType, previousRole, newRole, occurredAtUtc));
        }
    }
}
