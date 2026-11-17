using JasperFx;
using JasperFx.Events;
using Marten;
using SciFiLarpNetwork.Backend.Application.Players;
using SciFiLarpNetwork.Backend.Domain.Events.RunningNumberEvents;
using SciFiLarpNetwork.Backend.Infrastructure.Persistence.EventStore;
using SciFiLarpNetwork.Backend.Infrastructure.Players.PlayerDocuments;

namespace SciFiLarpNetwork.Backend.Infrastructure.Players;

/// <summary>Command steps shared by the player, character, and stat services.</summary>
internal static class RosterWrites
{
    extension(IDocumentSession session)
    {
        public async Task<(RosterOutcome Outcome, IEventStream<T>? Stream)> FetchExpectedAsync<T>(
            Guid id,
            long expectedVersion,
            CancellationToken cancellationToken)
            where T : class
        {
            try
            {
                var stream = await session.Events.FetchForWriting<T>(id, expectedVersion, cancellationToken);
                return stream.Aggregate is null ? (RosterOutcome.NotFound, null) : (RosterOutcome.Succeeded, stream);
            }
            catch (ConcurrencyException)
            {
                return await session.LoadAsync<T>(id, cancellationToken) is null
                    ? (RosterOutcome.NotFound, null)
                    : (RosterOutcome.StaleVersion, null);
            }
        }

        /// <summary>
        /// Takes a running-number sequence for writing. Concurrent creations both append to the sequence stream, so
        /// its version check rejects the later one instead of handing out the same number twice.
        /// </summary>
        public Task<IEventStream<RunningNumberDocument>> FetchRunningNumbersAsync(
            Guid sequenceId,
            CancellationToken cancellationToken) =>
            session.Events.FetchForWriting<RunningNumberDocument>(sequenceId, cancellationToken);

        /// <summary>
        /// Commits the command. <paramref name="lostRace"/> decides the outcome when a stream version check rejects
        /// the commit, and <paramref name="uniqueConflict"/> when a unique index does.
        /// </summary>
        public async Task<RosterOutcome> CommitAsync(
            Func<Task<RosterOutcome>> lostRace,
            Func<Task<RosterOutcome>> uniqueConflict,
            CancellationToken cancellationToken)
        {
            try
            {
                return await session.TrySaveChangesAsync(cancellationToken)
                    ? RosterOutcome.Succeeded
                    : await lostRace();
            }
            catch (Marten.Exceptions.ExistingStreamIdCollisionException)
            {
                // Two first-ever writes both started the same shared stream (a running-number sequence); the later one lost.
                return RosterOutcome.ConcurrentChange;
            }
            catch (DocumentAlreadyExistsException)
            {
                return await uniqueConflict();
            }
        }
    }

    extension(IEventStream<RunningNumberDocument> sequence)
    {
        public int IssueNext(Guid issuedTo, DateTimeOffset now)
        {
            var number = (sequence.Aggregate?.LastNumber ?? 0) + 1;
            sequence.AppendOne(new RunningNumberIssued(number, issuedTo, now));
            return number;
        }
    }

    private const int CreationAttempts = 3;

    /// <summary>
    /// Runs a creation again while it loses a race on a shared stream, such as a running-number sequence that another
    /// creation took first. Every attempt reads and checks everything again in a new session.
    /// </summary>
    public static async Task<RosterResult<T>> RetryLostRacesAsync<T>(Func<Task<RosterResult<T>>> creation)
        where T : class
    {
        var result = await creation();
        for (var attempt = 1; attempt < CreationAttempts && result.Outcome == RosterOutcome.ConcurrentChange; attempt++)
        {
            result = await creation();
        }

        return result;
    }

    public static Task<RosterOutcome> ConcurrentChange() => Task.FromResult(RosterOutcome.ConcurrentChange);

    public static Task<RosterOutcome> NameAlreadyUsed() => Task.FromResult(RosterOutcome.NameAlreadyUsed);

    extension<T>(RosterResult<T>)
        where T : class
    {
        public static RosterResult<T> Success(T value) => new(RosterOutcome.Succeeded, value);

        public static RosterResult<T> Failure(RosterOutcome outcome) => new(outcome, null);
    }
}
