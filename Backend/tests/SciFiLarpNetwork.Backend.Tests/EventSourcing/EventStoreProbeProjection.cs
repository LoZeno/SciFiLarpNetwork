using Marten.Events.Aggregation;
using SciFiLarpNetwork.Backend.Infrastructure.Persistence.EventStore;

namespace SciFiLarpNetwork.Backend.Tests.EventSourcing;

public sealed record EventStoreProbeState(Guid Id, int EventCount, long LastSequence);

public sealed partial class EventStoreProbeProjection : SingleStreamProjection<EventStoreProbeState, Guid>
{
    public static bool FailOnMarkedEvent { get; set; }

    public EventStoreProbeProjection()
    {
        Name = "EventStoreProbe";
    }

    public static EventStoreProbeState Create(PersistedDomainEvent domainEvent)
    {
        ThrowWhenRequested(domainEvent);
        return new EventStoreProbeState(domainEvent.StreamId, 1, domainEvent.Sequence);
    }

    public static EventStoreProbeState Apply(PersistedDomainEvent domainEvent, EventStoreProbeState state)
    {
        ThrowWhenRequested(domainEvent);
        return state with { EventCount = state.EventCount + 1, LastSequence = domainEvent.Sequence };
    }

    private static void ThrowWhenRequested(PersistedDomainEvent domainEvent)
    {
        if (FailOnMarkedEvent && domainEvent.Payload.TryGetProperty("failProjection", out var shouldFail) &&
            shouldFail.GetBoolean())
        {
            throw new InvalidOperationException("The test projection was instructed to fail.");
        }
    }
}
