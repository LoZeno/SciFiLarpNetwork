namespace SciFiLarpNetwork.Backend.Application.EventSourcing;

public sealed class EventStreamIntegrityException(Guid streamId, long expectedSequence)
    : Exception($"Event stream '{streamId}' is invalid at sequence {expectedSequence}.")
{
    public Guid StreamId { get; } = streamId;

    public long ExpectedSequence { get; } = expectedSequence;
}
