using SciFiLarpNetwork.Backend.Application.EventSourcing;

namespace SciFiLarpNetwork.Backend.Infrastructure.Persistence.EventStore;

internal sealed class AuthenticatedActorEventStreamStore(
    IEventStreamStore inner,
    IEventActorContext actorContext) : IEventStreamStore
{
    public Task<IReadOnlyList<IStoredEvent>> ReadStreamAsync(
        Guid streamId,
        CancellationToken cancellationToken = default) =>
        inner.ReadStreamAsync(streamId, cancellationToken);

    public Task<AppendResult> AppendAsync(
        Guid streamId,
        long expectedVersion,
        IReadOnlyList<object> events,
        Guid? commandId = null,
        CancellationToken cancellationToken = default)
    {
        if (!actorContext.RequiresAuthenticatedActor)
        {
            return inner.AppendAsync(streamId, expectedVersion, events, commandId, cancellationToken);
        }

        var actorId = actorContext.ActorId;
        if (string.IsNullOrWhiteSpace(actorId))
        {
            throw new UnauthorizedAccessException("An authenticated actor is required to append events from an HTTP request.");
        }

        // Actor attribution is handled by Marten's metadata via session.LastModifiedBy
        return inner.AppendAsync(streamId, expectedVersion, events, commandId, cancellationToken);
    }
}

internal sealed class BackgroundEventActorContext : IEventActorContext
{
    public bool RequiresAuthenticatedActor => false;

    public string? ActorId => null;
}