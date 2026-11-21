namespace SciFiLarpNetwork.Backend.Application.LarpEvents;

/// <summary>Events: scheduled LARP gatherings, their details, status, and archiving.</summary>
public interface ILarpEventService
{
    Task<LarpEventPage> GetEventsAsync(LarpEventQuery query, CancellationToken cancellationToken = default);

    Task<LarpEventSummary?> GetEventAsync(Guid eventId, CancellationToken cancellationToken = default);

    /// <summary>Creates an Open event without days, with the next event running number.</summary>
    Task<LarpEventResult<LarpEventSummary>> CreateEventAsync(LarpEventDefinition definition,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Replaces the details of an Open event if it is still at <paramref name="expectedVersion"/>; otherwise reports
    /// <see cref="LarpEventOutcome.StaleVersion"/> and changes nothing.
    /// </summary>
    Task<LarpEventResult<LarpEventSummary>> UpdateEventAsync(
        Guid eventId,
        long expectedVersion,
        LarpEventDefinition definition,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Closes an Open event that is still at <paramref name="expectedVersion"/> and, in the same command, gives each
    /// participant the experience of their <paramref name="awards"/>. Experience an earlier close already gave is
    /// never given again. A stale version reports <see cref="LarpEventOutcome.StaleVersion"/> and changes nothing.
    /// </summary>
    Task<LarpEventResult<LarpEventSummary>> CloseEventAsync(
        Guid eventId,
        long expectedVersion,
        IReadOnlyList<ClosingAward> awards,
        CancellationToken cancellationToken = default);

    /// <summary>Opens a Closed event again; an archived event must be unarchived first.</summary>
    Task<LarpEventResult<LarpEventSummary>> ReopenEventAsync(Guid eventId, CancellationToken cancellationToken = default);

    /// <summary>Archives a Closed event.</summary>
    Task<LarpEventResult<LarpEventSummary>> ArchiveEventAsync(Guid eventId, CancellationToken cancellationToken = default);

    Task<LarpEventResult<LarpEventSummary>> UnarchiveEventAsync(Guid eventId, CancellationToken cancellationToken = default);
}
