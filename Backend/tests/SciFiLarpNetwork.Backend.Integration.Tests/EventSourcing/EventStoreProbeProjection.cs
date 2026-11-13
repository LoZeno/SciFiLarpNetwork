using JasperFx.Events;
using Marten.Events.Aggregation;

namespace SciFiLarpNetwork.Backend.Tests.Integration.EventSourcing;

public sealed record EventStoreProbeState(Guid Id, int EventCount, long LastSequence);

public sealed record TestProbeEvent(
    string DisplayName,
    bool FailProjection);

public sealed partial class EventStoreProbeProjection : SingleStreamProjection<EventStoreProbeState, Guid>
{
    public static bool FailOnMarkedEvent { get; set; }

    public EventStoreProbeProjection()
    {
        Name = "EventStoreProbe";
        IncludeType<TestProbeEvent>();
    }

    public override EventStoreProbeState? Evolve(EventStoreProbeState? snapshot, Guid id, IEvent e)
    {
        if (e.Data is not TestProbeEvent probeEvent)
        {
            return snapshot;
        }

        ThrowWhenRequested(probeEvent);

        if (snapshot is null)
        {
            return new EventStoreProbeState(id, 1, e.Version);
        }

        return snapshot with { EventCount = snapshot.EventCount + 1, LastSequence = e.Version };
    }

    private static void ThrowWhenRequested(TestProbeEvent probeEvent)
    {
        if (FailOnMarkedEvent && probeEvent.FailProjection)
        {
            throw new InvalidOperationException("The test projection was instructed to fail.");
        }
    }
}
