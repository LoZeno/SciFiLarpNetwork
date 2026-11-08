namespace SciFiLarpNetwork.Backend.Application.EventSourcing;

public interface IEventStreamStore
{
    Task<IReadOnlyList<IStoredEvent>> ReadStreamAsync(
        Guid streamId,
        CancellationToken cancellationToken = default);

    Task<AppendResult> AppendAsync(
        Guid streamId,
        long expectedVersion,
        IReadOnlyList<object> events,
        Guid? commandId = null,
        CancellationToken cancellationToken = default);
}
