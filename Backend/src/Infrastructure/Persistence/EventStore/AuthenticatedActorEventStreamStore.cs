using SciFiLarpNetwork.Backend.Application.EventSourcing;
using SciFiLarpNetwork.Backend.Domain.Events;

namespace SciFiLarpNetwork.Backend.Infrastructure.Persistence.EventStore;

internal sealed class AuthenticatedActorEventStreamStore(
    IEventStreamStore inner,
    IEventActorContext actorContext) : IEventStreamStore
{
    public Task<IReadOnlyList<StoredDomainEvent>> ReadStreamAsync(
        Guid streamId,
        CancellationToken cancellationToken = default) =>
        inner.ReadStreamAsync(streamId, cancellationToken);

    public Task<AppendResult> AppendAsync(
        Guid streamId,
        long expectedVersion,
        IReadOnlyList<DomainEvent> events,
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

        var attributedEvents = events.Select(domainEvent => new DomainEvent(
            domainEvent.EventId,
            domainEvent.EventType,
            domainEvent.SchemaVersion,
            domainEvent.OccurredAtUtc,
            domainEvent.Payload,
            actorId,
            domainEvent.CorrelationId,
            domainEvent.CausationId)).ToArray();
        return inner.AppendAsync(streamId, expectedVersion, attributedEvents, commandId, cancellationToken);
    }
}

internal sealed class BackgroundEventActorContext : IEventActorContext
{
    public bool RequiresAuthenticatedActor => false;

    public string? ActorId => null;
}
