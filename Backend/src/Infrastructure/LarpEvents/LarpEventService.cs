using Marten;
using Marten.Pagination;
using SciFiLarpNetwork.Backend.Application.LarpEvents;
using SciFiLarpNetwork.Backend.Domain.Events.CharacterEvents;
using SciFiLarpNetwork.Backend.Domain.Events.LarpEventEvents;
using SciFiLarpNetwork.Backend.Domain.LarpEvents;
using SciFiLarpNetwork.Backend.Domain.Skills;
using SciFiLarpNetwork.Backend.Infrastructure.LarpEvents.LarpEventDocuments;
using SciFiLarpNetwork.Backend.Infrastructure.Players;
using SciFiLarpNetwork.Backend.Infrastructure.Players.PlayerDocuments;
using SciFiLarpNetwork.Backend.Infrastructure.Persistence.EventStore;

namespace SciFiLarpNetwork.Backend.Infrastructure.LarpEvents;

/// <summary>
/// Event commands follow Marten's command handler workflow: <c>FetchForWriting</c> on the event stream, then one
/// <c>SaveChangesAsync</c>, so a command that loses a race on the same event is rejected rather than overwriting it.
/// </summary>
internal sealed class LarpEventService(AttributedSessionFactory sessions, TimeProvider timeProvider) : ILarpEventService
{
    public async Task<LarpEventPage> GetEventsAsync(LarpEventQuery query, CancellationToken cancellationToken = default)
    {
        await using var session = sessions.QuerySession();
        IQueryable<LarpEventDocument> events = session.Query<LarpEventDocument>();
        if (!query.IncludeArchived)
        {
            events = events.Where(larpEvent => !larpEvent.IsArchived);
        }

        // PostgreSQL sorts nulls first in descending order, so events without days come first.
        var page = await events
            .OrderByDescending(larpEvent => larpEvent.FirstDayNumber)
            .ThenByDescending(larpEvent => larpEvent.Number)
            .ToPagedListAsync(query.PageNumber, LarpEventQuery.PageSize, cancellationToken);
        return new LarpEventPage(
            page.Select(larpEvent => larpEvent.ToSummary()).ToArray(),
            (int)page.PageNumber,
            Math.Max(1, (int)page.PageCount),
            page.TotalItemCount);
    }

    public async Task<LarpEventSummary?> GetEventAsync(Guid eventId, CancellationToken cancellationToken = default)
    {
        await using var session = sessions.QuerySession();
        return (await session.LoadAsync<LarpEventDocument>(eventId, cancellationToken))?.ToSummary();
    }

    public Task<LarpEventResult<LarpEventSummary>> CreateEventAsync(LarpEventDefinition definition,
        CancellationToken cancellationToken = default) =>
        LarpEventWrites.RetryLostRacesAsync(() => CreateEventOnceAsync(definition, cancellationToken));

    private async Task<LarpEventResult<LarpEventSummary>> CreateEventOnceAsync(LarpEventDefinition definition,
        CancellationToken cancellationToken)
    {
        if (LarpEventDefinitionValidator.Validate(definition) is { } invalid)
        {
            return LarpEventResult<LarpEventSummary>.Failure(invalid);
        }

        await using var session = sessions.OpenSession();
        var eventId = Guid.NewGuid();
        var sequence = await session.FetchRunningNumbersAsync(RunningNumberDocument.EventSequenceId, cancellationToken);
        var now = timeProvider.GetUtcNow();
        var number = sequence.IssueNext(eventId, now);
        session.Events.StartStream<LarpEventDocument>(
            eventId,
            new LarpEventCreated(
                eventId,
                number,
                definition.Name.Trim(),
                CatalogNames.Normalize(definition.Name),
                LarpEventTexts.Normalize(definition.Description),
                definition.BaseExperience,
                now));
        return await SaveAsync(session, eventId, editedVersion: null, cancellationToken);
    }

    public async Task<LarpEventResult<LarpEventSummary>> UpdateEventAsync(
        Guid eventId,
        long expectedVersion,
        LarpEventDefinition definition,
        CancellationToken cancellationToken = default)
    {
        if (LarpEventDefinitionValidator.Validate(definition) is { } invalid)
        {
            return LarpEventResult<LarpEventSummary>.Failure(invalid);
        }

        await using var session = sessions.OpenSession();
        var (outcome, stream) = await session.FetchExpectedEventAsync(eventId, expectedVersion, cancellationToken);
        if (stream?.Aggregate is not { } current)
        {
            return LarpEventResult<LarpEventSummary>.Failure(outcome);
        }

        if (current.Status == LarpEventStatus.Closed)
        {
            return LarpEventResult<LarpEventSummary>.Failure(LarpEventOutcome.EventClosed);
        }

        var changed = new LarpEventDetailsChanged(
            eventId,
            definition.Name.Trim(),
            CatalogNames.Normalize(definition.Name),
            LarpEventTexts.Normalize(definition.Description),
            definition.BaseExperience,
            timeProvider.GetUtcNow());
        if (changed.Name == current.Name && changed.Description == current.Description &&
            changed.BaseExperience == current.BaseExperience)
        {
            return LarpEventResult<LarpEventSummary>.Success(current.ToSummary());
        }

        stream.AppendOne(changed);
        return await SaveAsync(session, eventId, expectedVersion, cancellationToken);
    }

    /// <remarks>
    /// The event is taken at the version the closing grid was built from, so a registration or value changed since
    /// then makes the close stale instead of paying from an outdated grid. Every character that gets experience is
    /// also taken for writing, so a character edit at the same moment conflicts with the close.
    /// </remarks>
    public async Task<LarpEventResult<LarpEventSummary>> CloseEventAsync(
        Guid eventId,
        long expectedVersion,
        IReadOnlyList<ClosingAward> awards,
        CancellationToken cancellationToken = default)
    {
        await using var session = sessions.OpenSession();
        var (fetchOutcome, stream) = await session.FetchExpectedEventAsync(eventId, expectedVersion, cancellationToken);
        if (stream?.Aggregate is not { } current)
        {
            return LarpEventResult<LarpEventSummary>.Failure(fetchOutcome);
        }

        if (current.Status == LarpEventStatus.Closed)
        {
            return LarpEventResult<LarpEventSummary>.Success(current.ToSummary());
        }

        var (grantOutcome, grants) = ClosingGrants.From(current, awards);
        if (grantOutcome != LarpEventOutcome.Succeeded)
        {
            return LarpEventResult<LarpEventSummary>.Failure(grantOutcome);
        }

        var now = timeProvider.GetUtcNow();
        foreach (var grant in grants)
        {
            if (await AppendGrantAsync(session, stream, grant, now, cancellationToken) is var granted and not LarpEventOutcome.Succeeded)
            {
                return LarpEventResult<LarpEventSummary>.Failure(granted);
            }
        }

        stream.AppendOne(new LarpEventClosed(eventId, now));
        return await SaveAsync(session, eventId, expectedVersion, cancellationToken);
    }

    /// <summary>
    /// Records the grant on the event and, when it gives any experience, adds it to the character. A grant of 0 is
    /// still recorded, so a later close shows those cells as given.
    /// </summary>
    private static async Task<LarpEventOutcome> AppendGrantAsync(
        IDocumentSession session,
        JasperFx.Events.IEventStream<LarpEventDocument> stream,
        ClosingGrant grant,
        DateTimeOffset now,
        CancellationToken cancellationToken)
    {
        if (grant.Amount > 0)
        {
            var character = await session.Events.FetchForWriting<CharacterDocument>(grant.CharacterId, cancellationToken);
            if (character.Aggregate is not { } current)
            {
                return LarpEventOutcome.UnknownCharacter;
            }

            if (ExperienceChanges.Apply(current.ExperiencePoints, grant.Amount) is not { } experiencePoints)
            {
                return LarpEventOutcome.ExperienceOutOfRange;
            }

            character.AppendOne(new CharacterExperienceChanged(grant.CharacterId, stream.Id, grant.Amount, experiencePoints, now));
        }

        stream.AppendOne(new ClosingExperienceAwarded(stream.Id, grant.CharacterId, grant.BaseExperience, grant.DayIds,
            grant.Amount, now));
        return LarpEventOutcome.Succeeded;
    }

    public async Task<LarpEventResult<LarpEventSummary>> ReopenEventAsync(Guid eventId,
        CancellationToken cancellationToken = default)
    {
        await using var session = sessions.OpenSession();
        var stream = await session.Events.FetchForWriting<LarpEventDocument>(eventId, cancellationToken);
        switch (stream.Aggregate)
        {
            case null:
                return LarpEventResult<LarpEventSummary>.Failure(LarpEventOutcome.NotFound);
            case { Status: LarpEventStatus.Open } current:
                return LarpEventResult<LarpEventSummary>.Success(current.ToSummary());
            case { IsArchived: true }:
                return LarpEventResult<LarpEventSummary>.Failure(LarpEventOutcome.Archived);
        }

        stream.AppendOne(new LarpEventReopened(eventId, timeProvider.GetUtcNow()));
        return await SaveAsync(session, eventId, editedVersion: null, cancellationToken);
    }

    public async Task<LarpEventResult<LarpEventSummary>> ArchiveEventAsync(Guid eventId,
        CancellationToken cancellationToken = default)
    {
        await using var session = sessions.OpenSession();
        var stream = await session.Events.FetchForWriting<LarpEventDocument>(eventId, cancellationToken);
        switch (stream.Aggregate)
        {
            case null:
                return LarpEventResult<LarpEventSummary>.Failure(LarpEventOutcome.NotFound);
            case { IsArchived: true } current:
                return LarpEventResult<LarpEventSummary>.Success(current.ToSummary());
            case { Status: LarpEventStatus.Open }:
                return LarpEventResult<LarpEventSummary>.Failure(LarpEventOutcome.EventNotClosed);
        }

        stream.AppendOne(new LarpEventArchived(eventId, timeProvider.GetUtcNow()));
        return await SaveAsync(session, eventId, editedVersion: null, cancellationToken);
    }

    public async Task<LarpEventResult<LarpEventSummary>> UnarchiveEventAsync(Guid eventId,
        CancellationToken cancellationToken = default)
    {
        await using var session = sessions.OpenSession();
        var stream = await session.Events.FetchForWriting<LarpEventDocument>(eventId, cancellationToken);
        switch (stream.Aggregate)
        {
            case null:
                return LarpEventResult<LarpEventSummary>.Failure(LarpEventOutcome.NotFound);
            case { IsArchived: false } current:
                return LarpEventResult<LarpEventSummary>.Success(current.ToSummary());
        }

        stream.AppendOne(new LarpEventUnarchived(eventId, timeProvider.GetUtcNow()));
        return await SaveAsync(session, eventId, editedVersion: null, cancellationToken);
    }

    /// <summary>
    /// Commits the command. A rejected commit is <see cref="LarpEventOutcome.StaleVersion"/> only when the edited
    /// event itself moved on from <paramref name="editedVersion"/>; otherwise it is a concurrent change.
    /// </summary>
    private async Task<LarpEventResult<LarpEventSummary>> SaveAsync(
        IDocumentSession session,
        Guid eventId,
        long? editedVersion,
        CancellationToken cancellationToken)
    {
        var outcome = await session.CommitEventAsync(
            () => LostRaceOutcomeAsync(eventId, editedVersion, cancellationToken),
            cancellationToken);
        if (outcome != LarpEventOutcome.Succeeded)
        {
            return LarpEventResult<LarpEventSummary>.Failure(outcome);
        }

        var saved = await session.LoadAsync<LarpEventDocument>(eventId, cancellationToken);
        return LarpEventResult<LarpEventSummary>.Success(saved!.ToSummary());
    }

    private async Task<LarpEventOutcome> LostRaceOutcomeAsync(Guid eventId, long? editedVersion,
        CancellationToken cancellationToken)
    {
        if (editedVersion is not { } version)
        {
            return LarpEventOutcome.ConcurrentChange;
        }

        await using var query = sessions.QuerySession();
        return await query.LoadAsync<LarpEventDocument>(eventId, cancellationToken) is { } current &&
               current.Version == version
            ? LarpEventOutcome.ConcurrentChange
            : LarpEventOutcome.StaleVersion;
    }
}
