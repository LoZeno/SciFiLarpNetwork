using System.Security.Cryptography;
using System.Text.Json;
using JasperFx.Events;
using Marten;
using Npgsql;
using SciFiLarpNetwork.Backend.Application.EventSourcing;

namespace SciFiLarpNetwork.Backend.Infrastructure.Persistence.EventStore;

internal sealed class AuthenticatedActorEventStreamStore(
    IDocumentStore documentStore,
    IEventActorContext actorContext) : IEventStreamStore
{
    public async Task<IReadOnlyList<IStoredEvent>> ReadStreamAsync(
        Guid streamId,
        CancellationToken cancellationToken = default)
    {
        await using var session = documentStore.QuerySession();
        var events = await session.Events.FetchStreamAsync(streamId, token: cancellationToken);
        return events.Select(e => new MartenStoredEvent(e)).ToArray();
    }

    public async Task<AppendResult> AppendAsync(
        Guid streamId,
        long expectedVersion,
        IReadOnlyList<object> events,
        Guid? commandId = null,
        CancellationToken cancellationToken = default)
    {
        if (actorContext.RequiresAuthenticatedActor)
        {
            var actorId = actorContext.ActorId;
            if (string.IsNullOrWhiteSpace(actorId))
            {
                throw new UnauthorizedAccessException("An authenticated actor is required to append events from an HTTP request.");
            }
        }

        var requestHash = ComputeRequestHash(streamId, expectedVersion, events);
        await using var session = documentStore.LightweightSession();

        if (actorContext.RequiresAuthenticatedActor)
        {
            session.LastModifiedBy = actorContext.ActorId;
        }

        if (commandId is { } id)
        {
            var receipt = await session.LoadAsync<CommandReceiptDocument>(id, cancellationToken);
            if (receipt is not null)
            {
                if (!string.Equals(receipt.RequestHash, requestHash, StringComparison.Ordinal))
                {
                    throw new IdempotencyKeyReuseException(id);
                }
                return new AppendResult(receipt.StreamVersion, receipt.EventIds, WasDuplicate: true);
            }
        }

        if (events.Count == 0)
        {
            throw new ArgumentException("At least one event is required.", nameof(events));
        }

        JasperFx.Events.IEventStream<EmptyAggregate> stream;
        try
        {
            // Use FetchForWriting as recommended by MartenDB documentation
            stream = await session.Events.FetchForWriting<EmptyAggregate>(streamId, expectedVersion, cancellationToken);
        }
        catch (JasperFx.ConcurrencyException ex)
        {
            throw new StreamConcurrencyException(streamId, expectedVersion, ex);
        }

        foreach (var @event in events)
        {
            stream.AppendOne(@event);
        }

        var eventIds = stream.Events.Select(e => e.Id).ToArray();

        foreach (var eventId in eventIds)
        {
            session.Insert(new GlobalEventIdDocument(eventId, streamId));
        }

        var streamVersion = expectedVersion + events.Count;
        if (commandId is { } command)
        {
            session.Store(new CommandReceiptDocument(command, streamId, streamVersion, requestHash, eventIds));
        }

        try
        {
            await session.SaveChangesAsync(cancellationToken);
        }
        catch (Marten.Exceptions.ExistingStreamIdCollisionException ex)
        {
            throw new StreamConcurrencyException(streamId, expectedVersion, ex);
        }
        catch (JasperFx.ConcurrencyException ex)
        {
            throw new StreamConcurrencyException(streamId, expectedVersion, ex);
        }
        catch (Npgsql.PostgresException ex) when (IsStreamIdentityCollision(ex))
        {
            throw new StreamConcurrencyException(streamId, expectedVersion, ex);
        }

        return new AppendResult(streamVersion, eventIds, WasDuplicate: false);
    }

    private static string ComputeRequestHash(Guid streamId, long expectedVersion, IReadOnlyList<object> events)
    {
        var bytes = JsonSerializer.SerializeToUtf8Bytes(new { streamId, expectedVersion, events });
        return Convert.ToHexString(SHA256.HashData(bytes));
    }

    private static bool IsStreamIdentityCollision(Exception exception)
    {
        for (var current = exception; current is not null; current = current.InnerException)
        {
            if (current is Npgsql.PostgresException { SqlState: "23505", ConstraintName: "pkey_mt_streams_id" })
            {
                return true;
            }
        }
        return false;
    }
}

// Minimal self-aggregating type for FetchForWriting, as required by MartenDB documentation
public sealed record EmptyAggregate(Guid Id, long Version = 0)
{
    public static EmptyAggregate Create(object _, Guid streamId) => new(streamId);
    public static EmptyAggregate Apply(object _, EmptyAggregate aggregate) => aggregate with { Version = aggregate.Version + 1 };
}

internal sealed class BackgroundEventActorContext : IEventActorContext
{
    public bool RequiresAuthenticatedActor => false;

    public string? ActorId => null;
}

internal sealed class MartenStoredEvent(IEvent inner) : IStoredEvent
{
    public Guid EventId => inner.Id;
    public Guid StreamId => inner.StreamId;
    // IEvent.Version is the event's position within its stream (mt_events.version);
    // IEvent.Sequence is the store-wide sequence (mt_events.seq_id).
    public long Sequence => inner.Version;
    public string EventType => inner.Data.GetType().FullName!;
    public int SchemaVersion => 1;
    public DateTimeOffset OccurredAtUtc => inner.Data is { } data && data.GetType().GetProperty("OccurredAtUtc")?.GetValue(data) is DateTimeOffset dt ? dt : DateTimeOffset.UtcNow;
    public DateTimeOffset TimestampUtc => inner.Timestamp.ToUniversalTime();
    public string? ActorId => inner.UserName;
    public string? CorrelationId => inner.CorrelationId;
    public string? CausationId => inner.CausationId;
    public object Data => inner.Data;
}