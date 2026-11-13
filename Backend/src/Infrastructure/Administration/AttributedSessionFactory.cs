using Marten;
using SciFiLarpNetwork.Backend.Application.EventSourcing;

namespace SciFiLarpNetwork.Backend.Infrastructure.Administration;

internal sealed class AttributedSessionFactory(IDocumentStore documentStore, IEventActorContext actorContext)
{
    public IQuerySession QuerySession() => documentStore.QuerySession();

    /// <summary>
    /// Opens a lightweight session whose events are attributed through Marten's <c>LastModifiedBy</c>
    /// user-name metadata. HTTP-originated changes use the authenticated actor; <paramref name="verifiedActorId"/>
    /// is used only when the server itself has just established the actor (sign-in, or revoking an expired
    /// session during authentication), never for a client-supplied identity.
    /// </summary>
    public IDocumentSession OpenSession(string? verifiedActorId = null)
    {
        var actorId = actorContext.ActorId ?? verifiedActorId;
        if (actorContext.RequiresAuthenticatedActor && string.IsNullOrWhiteSpace(actorId))
        {
            throw new UnauthorizedAccessException("An authenticated actor is required to append events from an HTTP request.");
        }

        var session = documentStore.LightweightSession();
        session.LastModifiedBy = actorId;
        return session;
    }
}
