namespace SciFiLarpNetwork.Backend.Application.LarpEvents;

/// <summary>The days of an Open event. Dates are unique within an event.</summary>
public interface ILarpEventDayService
{
    /// <param name="experience">The experience the day can give when the event closes; <see langword="null"/> for none.</param>
    Task<LarpEventResult<LarpEventSummary>> AddDayAsync(Guid eventId, DateOnly dayDate, int? experience,
        CancellationToken cancellationToken = default);

    /// <summary>Changes a day's date and experience; its registrations stay with it.</summary>
    Task<LarpEventResult<LarpEventSummary>> UpdateDayAsync(Guid eventId, Guid dayId, DateOnly dayDate, int? experience,
        CancellationToken cancellationToken = default);

    /// <summary>Removes a day that has no registered characters.</summary>
    Task<LarpEventResult<LarpEventSummary>> RemoveDayAsync(Guid eventId, Guid dayId,
        CancellationToken cancellationToken = default);
}
