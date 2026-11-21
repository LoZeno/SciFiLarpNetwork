namespace SciFiLarpNetwork.Backend.Application.LarpEvents;

/// <summary>The days of an Open event. Dates are unique within an event.</summary>
public interface ILarpEventDayService
{
    Task<LarpEventResult<LarpEventSummary>> AddDayAsync(Guid eventId, DateOnly dayDate,
        CancellationToken cancellationToken = default);

    /// <summary>Moves a day to another date; its registrations stay with it.</summary>
    Task<LarpEventResult<LarpEventSummary>> RescheduleDayAsync(Guid eventId, Guid dayId, DateOnly dayDate,
        CancellationToken cancellationToken = default);

    /// <summary>Removes a day that has no registered characters.</summary>
    Task<LarpEventResult<LarpEventSummary>> RemoveDayAsync(Guid eventId, Guid dayId,
        CancellationToken cancellationToken = default);
}
