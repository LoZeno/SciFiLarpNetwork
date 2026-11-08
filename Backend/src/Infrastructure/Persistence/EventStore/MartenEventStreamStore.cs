using System.Security.Cryptography;
using System.Text.Json;
using JasperFx.Events;
using Marten;
using Npgsql;
using SciFiLarpNetwork.Backend.Application.EventSourcing;

namespace SciFiLarpNetwork.Backend.Infrastructure.Persistence.EventStore;

internal sealed class MartenEventStreamStore(IDocumentStore documentStore) : IEventStreamStore
{
    public async Task<IReadOnlyList<IStoredEvent>> ReadStreamAsync(
        Guid streamId,
        CancellationToken cancellationToken = default)
    {
        await using var session = documentStore.QuerySession();
        var events = await session.Events.FetchStreamAsync(streamId, token: cancellationToken);

        return events
            .Select(@event => new MartenStoredEvent(@event))
            .ToArray();
    }

    public async Task<AppendResult> AppendAsync(
        Guid streamId,
        long expectedVersion,
        IReadOnlyList<object> events,
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

        var eventIds = events.Select(_ => Guid.NewGuid()).ToArray();
        var stream = expectedVersion == 0
            ? session.Events.StartStream(streamId, events.ToArray())
            : session.Events.Append(streamId, expectedVersion, events.ToArray());

        for (var i = 0; i < stream.Events.Count; i++)
        {
            stream.Events[i].Id = eventIds[i];
        }

        foreach (var eventId in eventIds)
        {
            session.Insert(new GlobalEventIdDocument(eventId, streamId));
        }

        var streamVersion = expectedVersion + events.Count;
        if (commandId is { } command)
        {
            session.Store(new CommandReceiptDocument(
                command,
                streamId,
                streamVersion,
                requestHash,
                eventIds));
        }

        try
        {
            await session.SaveChangesAsync(cancellationToken);
        }
        catch (Marten.Exceptions.ExistingStreamIdCollisionException exception)
        {
            throw new StreamConcurrencyException(streamId, expectedVersion, exception);
        }
        catch (EventStreamUnexpectedMaxEventIdException exception)
        {
            throw new StreamConcurrencyException(streamId, expectedVersion, exception);
        }
        catch (PostgresException exception) when (IsStreamIdentityCollision(exception))
        {
            throw new StreamConcurrencyException(streamId, expectedVersion, exception);
        }

        return new AppendResult(streamVersion, eventIds, WasDuplicate: false);
    }

    private static string ComputeRequestHash(
        Guid streamId,
        long expectedVersion,
        IReadOnlyList<object> events)
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

internal sealed class MartenStoredEvent(IEvent inner) : IStoredEvent
{
    public Guid EventId => inner.Id;
    public Guid StreamId => inner.StreamId;
    public long Sequence => inner.Sequence;
    public string EventType => inner.Data.GetType().FullName!;
    public int SchemaVersion => 1;
    public DateTimeOffset OccurredAtUtc => inner.Data is { } data && data.GetType().GetProperty("OccurredAtUtc")?.GetValue(data) is DateTimeOffset dt ? dt : DateTimeOffset.UtcNow;
    public DateTimeOffset TimestampUtc => inner.Timestamp.ToUniversalTime();
    public string? ActorId => inner.UserName;
    public string? CorrelationId => inner.CorrelationId;
    public string? CausationId => inner.CausationId;
    public object Data => inner.Data;
}
