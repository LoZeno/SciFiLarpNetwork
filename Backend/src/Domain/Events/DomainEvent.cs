using System.Text.Json;

namespace SciFiLarpNetwork.Backend.Domain.Events;

public sealed record DomainEvent
{
    public DomainEvent(
        Guid eventId,
        string eventType,
        int schemaVersion,
        DateTimeOffset occurredAtUtc,
        JsonElement payload,
        string? actorId = null,
        string? correlationId = null,
        string? causationId = null)
    {
        if (eventId == Guid.Empty)
        {
            throw new ArgumentException("An event ID is required.", nameof(eventId));
        }

        if (string.IsNullOrWhiteSpace(eventType))
        {
            throw new ArgumentException("An event type is required.", nameof(eventType));
        }

        if (schemaVersion < 1)
        {
            throw new ArgumentOutOfRangeException(nameof(schemaVersion), "Schema versions must be positive.");
        }

        if (occurredAtUtc.Offset != TimeSpan.Zero)
        {
            throw new ArgumentException("Event timestamps must be UTC.", nameof(occurredAtUtc));
        }

        if (payload.ValueKind is JsonValueKind.Undefined)
        {
            throw new ArgumentException("An event payload is required.", nameof(payload));
        }

        EventId = eventId;
        EventType = eventType;
        SchemaVersion = schemaVersion;
        OccurredAtUtc = occurredAtUtc;
        Payload = payload.Clone();
        ActorId = actorId;
        CorrelationId = correlationId;
        CausationId = causationId;
    }

    public Guid EventId { get; }

    public string EventType { get; }

    public int SchemaVersion { get; }

    public DateTimeOffset OccurredAtUtc { get; }

    public JsonElement Payload { get; }

    public string? ActorId { get; }

    public string? CorrelationId { get; }

    public string? CausationId { get; }
}
