using JasperFx;
using JasperFx.Events;
using Marten;
using SciFiLarpNetwork.Backend.Application.LarpEvents;
using SciFiLarpNetwork.Backend.Domain.LarpEvents;
using SciFiLarpNetwork.Backend.Infrastructure.LarpEvents.LarpEventDocuments;
using SciFiLarpNetwork.Backend.Infrastructure.Persistence.EventStore;

namespace SciFiLarpNetwork.Backend.Infrastructure.LarpEvents;

/// <summary>Command steps shared by the event, day, and participant services.</summary>
internal static class LarpEventWrites
{
    private const int CreationAttempts = 3;

    extension(IDocumentSession session)
    {
        public async Task<(LarpEventOutcome Outcome, IEventStream<LarpEventDocument>? Stream)> FetchExpectedEventAsync(
            Guid eventId,
            long expectedVersion,
            CancellationToken cancellationToken)
        {
            try
            {
                var stream = await session.Events.FetchForWriting<LarpEventDocument>(eventId, expectedVersion, cancellationToken);
                return stream.Aggregate is null ? (LarpEventOutcome.NotFound, null) : (LarpEventOutcome.Succeeded, stream);
            }
            catch (ConcurrencyException)
            {
                return await session.LoadAsync<LarpEventDocument>(eventId, cancellationToken) is null
                    ? (LarpEventOutcome.NotFound, null)
                    : (LarpEventOutcome.StaleVersion, null);
            }
        }

        /// <summary>
        /// Takes an Open event for writing. A command that loses a race on it is rejected at commit by Marten's
        /// version check, so it never acts on rules that changed meanwhile.
        /// </summary>
        public async Task<(LarpEventOutcome Outcome, IEventStream<LarpEventDocument>? Stream)> FetchOpenEventAsync(
            Guid eventId,
            CancellationToken cancellationToken)
        {
            var stream = await session.Events.FetchForWriting<LarpEventDocument>(eventId, cancellationToken);
            return stream.Aggregate switch
            {
                null => (LarpEventOutcome.NotFound, null),
                { Status: LarpEventStatus.Closed } => (LarpEventOutcome.EventClosed, null),
                _ => (LarpEventOutcome.Succeeded, stream)
            };
        }

        /// <summary>Commits the command; <paramref name="lostRace"/> decides the outcome when a version check rejects it.</summary>
        public async Task<LarpEventOutcome> CommitEventAsync(
            Func<Task<LarpEventOutcome>> lostRace,
            CancellationToken cancellationToken)
        {
            try
            {
                return await session.TrySaveChangesAsync(cancellationToken)
                    ? LarpEventOutcome.Succeeded
                    : await lostRace();
            }
            catch (Marten.Exceptions.ExistingStreamIdCollisionException)
            {
                // Two first-ever creations both started the event running-number sequence; the later one lost.
                return LarpEventOutcome.ConcurrentChange;
            }
            catch (DocumentAlreadyExistsException)
            {
                return LarpEventOutcome.ConcurrentChange;
            }
        }
    }

    extension<T>(LarpEventResult<T>)
        where T : class
    {
        public static LarpEventResult<T> Success(T value) => new(LarpEventOutcome.Succeeded, value);

        public static LarpEventResult<T> Failure(LarpEventOutcome outcome) => new(outcome, null);
    }

    public static Task<LarpEventOutcome> ConcurrentChange() => Task.FromResult(LarpEventOutcome.ConcurrentChange);

    /// <summary>Runs a creation again while it loses a race on the running-number sequence.</summary>
    public static async Task<LarpEventResult<T>> RetryLostRacesAsync<T>(Func<Task<LarpEventResult<T>>> creation)
        where T : class
    {
        var result = await creation();
        for (var attempt = 1; attempt < CreationAttempts && result.Outcome == LarpEventOutcome.ConcurrentChange; attempt++)
        {
            result = await creation();
        }

        return result;
    }
}
