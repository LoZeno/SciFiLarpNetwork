namespace SciFiLarpNetwork.Backend.Application.EventSourcing;

public sealed class StreamConcurrencyException(Guid streamId, long expectedVersion, Exception innerException)
    : Exception($"Event stream '{streamId}' changed after version {expectedVersion} was read.", innerException)
{
    public Guid StreamId { get; } = streamId;

    public long ExpectedVersion { get; } = expectedVersion;
}
