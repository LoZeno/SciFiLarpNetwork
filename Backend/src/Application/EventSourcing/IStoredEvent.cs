namespace SciFiLarpNetwork.Backend.Application.EventSourcing;

public interface IStoredEvent
{
    Guid EventId { get; }
    Guid StreamId { get; }
    long Sequence { get; }
    string EventType { get; }
    int SchemaVersion { get; }
    DateTimeOffset OccurredAtUtc { get; }
    DateTimeOffset TimestampUtc { get; }
    string? ActorId { get; }
    string? CorrelationId { get; }
    string? CausationId { get; }
    object Data { get; }
}
