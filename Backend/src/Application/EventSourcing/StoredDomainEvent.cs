using System.Text.Json;

namespace SciFiLarpNetwork.Backend.Application.EventSourcing;

public sealed record StoredDomainEvent(
    Guid EventId,
    Guid StreamId,
    long Sequence,
    string EventType,
    int SchemaVersion,
    DateTimeOffset OccurredAtUtc,
    DateTimeOffset RecordedAtUtc,
    string? ActorId,
    string? CorrelationId,
    string? CausationId,
    JsonElement Payload);
