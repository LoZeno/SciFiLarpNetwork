using Marten;
using SciFiLarpNetwork.Backend.Application.LarpEvents;
using SciFiLarpNetwork.Backend.Domain.Events.LarpEventEvents;
using SciFiLarpNetwork.Backend.Domain.LarpEvents;
using SciFiLarpNetwork.Backend.Infrastructure.LarpEvents.LarpEventDocuments;
using SciFiLarpNetwork.Backend.Infrastructure.Persistence.EventStore;

namespace SciFiLarpNetwork.Backend.Infrastructure.LarpEvents;

/// <summary>
/// Day commands take the event stream for writing, so two changes to the same event's days cannot both pass the
/// unique-date or no-participants checks.
/// </summary>
internal sealed class LarpEventDayService(AttributedSessionFactory sessions, TimeProvider timeProvider) : ILarpEventDayService
{
    public async Task<LarpEventResult<LarpEventSummary>> AddDayAsync(Guid eventId, DateOnly dayDate, int? experience,
        CancellationToken cancellationToken = default)
    {
        if (!ExperienceChanges.IsValidValue(experience))
        {
            return Failure(LarpEventOutcome.InvalidExperienceValue);
        }

        await using var session = sessions.OpenSession();
        var (outcome, stream) = await session.FetchOpenEventAsync(eventId, cancellationToken);
        if (stream?.Aggregate is not { } current)
        {
            return Failure(outcome);
        }

        if (current.Days.Any(day => day.Date == dayDate))
        {
            return Failure(LarpEventOutcome.DuplicateDate);
        }

        stream.AppendOne(new LarpEventDayAdded(eventId, Guid.NewGuid(), dayDate, experience, timeProvider.GetUtcNow()));
        return await SaveAsync(session, eventId, cancellationToken);
    }

    public async Task<LarpEventResult<LarpEventSummary>> UpdateDayAsync(Guid eventId, Guid dayId, DateOnly dayDate,
        int? experience, CancellationToken cancellationToken = default)
    {
        if (!ExperienceChanges.IsValidValue(experience))
        {
            return Failure(LarpEventOutcome.InvalidExperienceValue);
        }

        await using var session = sessions.OpenSession();
        var (outcome, stream) = await session.FetchOpenEventAsync(eventId, cancellationToken);
        if (stream?.Aggregate is not { } current)
        {
            return Failure(outcome);
        }

        if (current.Days.FirstOrDefault(day => day.DayId == dayId) is not { } changed)
        {
            return Failure(LarpEventOutcome.UnknownDay);
        }

        if (changed.Date == dayDate && changed.Experience == experience)
        {
            return LarpEventResult<LarpEventSummary>.Success(current.ToSummary());
        }

        if (current.Days.Any(day => day.DayId != dayId && day.Date == dayDate))
        {
            return Failure(LarpEventOutcome.DuplicateDate);
        }

        stream.AppendOne(new LarpEventDayChanged(eventId, dayId, dayDate, experience, timeProvider.GetUtcNow()));
        return await SaveAsync(session, eventId, cancellationToken);
    }

    public async Task<LarpEventResult<LarpEventSummary>> RemoveDayAsync(Guid eventId, Guid dayId,
        CancellationToken cancellationToken = default)
    {
        await using var session = sessions.OpenSession();
        var (outcome, stream) = await session.FetchOpenEventAsync(eventId, cancellationToken);
        if (stream?.Aggregate is not { } current)
        {
            return Failure(outcome);
        }

        if (!current.HasDay(dayId))
        {
            return Failure(LarpEventOutcome.UnknownDay);
        }

        if (current.Participants.Any(participant => participant.DayIds.Contains(dayId)))
        {
            return Failure(LarpEventOutcome.DayHasParticipants);
        }

        stream.AppendOne(new LarpEventDayRemoved(eventId, dayId, timeProvider.GetUtcNow()));
        return await SaveAsync(session, eventId, cancellationToken);
    }

    private static async Task<LarpEventResult<LarpEventSummary>> SaveAsync(
        IDocumentSession session,
        Guid eventId,
        CancellationToken cancellationToken)
    {
        var outcome = await session.CommitEventAsync(LarpEventWrites.ConcurrentChange, cancellationToken);
        if (outcome != LarpEventOutcome.Succeeded)
        {
            return Failure(outcome);
        }

        var saved = await session.LoadAsync<LarpEventDocument>(eventId, cancellationToken);
        return LarpEventResult<LarpEventSummary>.Success(saved!.ToSummary());
    }

    private static LarpEventResult<LarpEventSummary> Failure(LarpEventOutcome outcome) =>
        LarpEventResult<LarpEventSummary>.Failure(outcome);
}
