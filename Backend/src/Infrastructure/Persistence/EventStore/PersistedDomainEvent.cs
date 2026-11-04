using System.Text.Json;

namespace SciFiLarpNetwork.Backend.Infrastructure.Persistence.EventStore;

public sealed record PersistedDomainEvent(
    Guid EventId,
    Guid StreamId,
    long Sequence,
    string EventType,
    int SchemaVersion,
    DateTimeOffset OccurredAtUtc,
    string? ActorId,
    string? CorrelationId,
    string? CausationId,
    JsonElement Payload);
