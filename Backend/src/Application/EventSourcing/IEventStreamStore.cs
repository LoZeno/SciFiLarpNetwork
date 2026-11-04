using SciFiLarpNetwork.Backend.Domain.Events;

namespace SciFiLarpNetwork.Backend.Application.EventSourcing;

public interface IEventStreamStore
{
    Task<IReadOnlyList<StoredDomainEvent>> ReadStreamAsync(
        Guid streamId,
        CancellationToken cancellationToken = default);

    Task<AppendResult> AppendAsync(
        Guid streamId,
        long expectedVersion,
        IReadOnlyList<DomainEvent> events,
        Guid? commandId = null,
        CancellationToken cancellationToken = default);
}
