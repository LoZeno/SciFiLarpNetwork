using System.Security.Cryptography;
using System.Text.Json;
using JasperFx.Events;
using Marten;
using Npgsql;
using SciFiLarpNetwork.Backend.Application.EventSourcing;
using SciFiLarpNetwork.Backend.Domain.Events;

namespace SciFiLarpNetwork.Backend.Infrastructure.Persistence.EventStore;

internal sealed class MartenEventStreamStore(IDocumentStore documentStore, EventUpcasterRegistry upcasters) : IEventStreamStore
{
    public async Task<IReadOnlyList<StoredDomainEvent>> ReadStreamAsync(
        Guid streamId,
        CancellationToken cancellationToken = default)
    {
        await using var session = documentStore.QuerySession();
        var events = await session.Events.FetchStreamAsync(streamId, token: cancellationToken);

        return events
            .Select(@event =>
            {
                var persisted = (PersistedDomainEvent)@event.Data;
                var (upcastedSchemaVersion, upcastedPayload) =
                    upcasters.Upcast(persisted.EventType, persisted.SchemaVersion, persisted.Payload);
                return new StoredDomainEvent(
                    persisted.EventId,
                    persisted.StreamId,
                    persisted.Sequence,
                    persisted.EventType,
                    upcastedSchemaVersion,
                    persisted.OccurredAtUtc,
                    @event.Timestamp.ToUniversalTime(),
                    @event.UserName,
                    @event.CorrelationId,
                    @event.CausationId,
                    upcastedPayload.Clone());
            })
            .ToArray();
    }

    public async Task<AppendResult> AppendAsync(
        Guid streamId,
        long expectedVersion,
        IReadOnlyList<DomainEvent> events,
        Guid? commandId = null,
        CancellationToken cancellationToken = default)
    {
        ArgumentOutOfRangeException.ThrowIfEqual(streamId, Guid.Empty);
        ArgumentOutOfRangeException.ThrowIfNegative(expectedVersion);
        ArgumentNullException.ThrowIfNull(events);
        if (events.Count == 0)
        {
            throw new ArgumentException("At least one event is required.", nameof(events));
        }

        if (events.Select(domainEvent => domainEvent.EventId).Distinct().Count() != events.Count)
        {
            throw new ArgumentException("An append batch cannot contain duplicate event IDs.", nameof(events));
        }

        var requestHash = ComputeRequestHash(streamId, expectedVersion, events);
        await using var session = documentStore.LightweightSession();
        if (commandId is { } id)
        {
            var receipt = await session.LoadAsync<CommandReceiptDocument>(id, cancellationToken);
            if (receipt is not null)
            {
                if (!StringComparer.Ordinal.Equals(receipt.RequestHash, requestHash))
                {
                    throw new IdempotencyKeyReuseException(id);
                }

                return new AppendResult(receipt.StreamVersion, receipt.EventIds, WasDuplicate: true);
            }
        }

        session.LastModifiedBy = events[0].ActorId;
        session.CorrelationId = events[0].CorrelationId;
        session.CausationId = events[0].CausationId;

        var persistedEvents = events.Select((domainEvent, index) => new PersistedDomainEvent(
            domainEvent.EventId,
            streamId,
            expectedVersion + index + 1,
            domainEvent.EventType,
            domainEvent.SchemaVersion,
            domainEvent.OccurredAtUtc,
            domainEvent.ActorId,
            domainEvent.CorrelationId,
            domainEvent.CausationId,
            domainEvent.Payload.Clone())).ToArray();

        var stream = session.Events.Append(streamId, expectedVersion, persistedEvents);
        foreach (var (eventEnvelope, domainEvent) in stream.Events.Zip(events))
        {
            eventEnvelope.Id = domainEvent.EventId;
            eventEnvelope.UserName = domainEvent.ActorId;
            eventEnvelope.CorrelationId = domainEvent.CorrelationId;
            eventEnvelope.CausationId = domainEvent.CausationId;
        }

        foreach (var domainEvent in events)
        {
            session.Insert(new GlobalEventIdDocument(domainEvent.EventId, streamId));
        }

        var streamVersion = expectedVersion + events.Count;
        if (commandId is { } command)
        {
            session.Store(new CommandReceiptDocument(
                command,
                streamId,
                streamVersion,
                requestHash,
                events.Select(domainEvent => domainEvent.EventId).ToArray()));
        }

        try
        {
            await session.SaveChangesAsync(cancellationToken);
        }
        catch (Exception exception) when (
            exception is Marten.Exceptions.ExistingStreamIdCollisionException or EventStreamUnexpectedMaxEventIdException ||
            IsStreamIdentityCollision(exception))
        {
            throw new StreamConcurrencyException(streamId, expectedVersion, exception);
        }

        return new AppendResult(
            streamVersion,
            events.Select(domainEvent => domainEvent.EventId).ToArray(),
            WasDuplicate: false);
    }

    private static string ComputeRequestHash(
        Guid streamId,
        long expectedVersion,
        IReadOnlyList<DomainEvent> events)
    {
        var bytes = JsonSerializer.SerializeToUtf8Bytes(new { streamId, expectedVersion, events });
        return Convert.ToHexString(SHA256.HashData(bytes));
    }

    private static bool IsStreamIdentityCollision(Exception exception)
    {
        for (var current = exception; current is not null; current = current.InnerException)
        {
            if (current is PostgresException
                {
                    SqlState: PostgresErrorCodes.UniqueViolation,
                    ConstraintName: "pkey_mt_streams_id"
                })
            {
                return true;
            }
        }

        return false;
    }
}
