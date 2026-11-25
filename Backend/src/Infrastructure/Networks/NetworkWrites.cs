using JasperFx;
using JasperFx.Events;
using Marten;
using SciFiLarpNetwork.Backend.Application.Networks;
using SciFiLarpNetwork.Backend.Infrastructure.Networks.NetworkDocuments;
using SciFiLarpNetwork.Backend.Infrastructure.Persistence.EventStore;

namespace SciFiLarpNetwork.Backend.Infrastructure.Networks;

/// <summary>Command steps shared by the network services.</summary>
internal static class NetworkWrites
{
    extension(IDocumentSession session)
    {
        public async Task<(NetworkOutcome Outcome, IEventStream<NetworkDocument>? Stream)> FetchExpectedNetworkAsync(
            Guid networkId,
            long expectedVersion,
            CancellationToken cancellationToken)
        {
            try
            {
                var stream =
                    await session.Events.FetchForWriting<NetworkDocument>(networkId, expectedVersion,
                        cancellationToken);
                return stream.Aggregate is null ? (NetworkOutcome.NotFound, null) : (NetworkOutcome.Succeeded, stream);
            }
            catch (ConcurrencyException)
            {
                return await session.LoadAsync<NetworkDocument>(networkId, cancellationToken) is null
                    ? (NetworkOutcome.NotFound, null)
                    : (NetworkOutcome.StaleVersion, null);
            }
        }

        /// <summary>
        /// Commits the command and returns the saved network. <paramref name="uniqueConflict"/> decides which
        /// unique value a concurrent command claimed first when a unique index rejects the commit.
        /// </summary>
        public async Task<NetworkResult> SaveNetworkAsync(
            Guid networkId,
            Func<Task<NetworkOutcome>> uniqueConflict,
            CancellationToken cancellationToken)
        {
            var outcome = await session.CommitNetworkAsync(uniqueConflict, cancellationToken);
            return outcome == NetworkOutcome.Succeeded
                ? await session.LoadSavedNetworkAsync(networkId, cancellationToken)
                : NetworkResult.Failure(outcome);
        }

        /// <summary>
        /// Commits the command. <paramref name="uniqueConflict"/> decides which unique value a concurrent command
        /// claimed first when a unique index rejects the commit.
        /// </summary>
        public async Task<NetworkOutcome> CommitNetworkAsync(
            Func<Task<NetworkOutcome>> uniqueConflict,
            CancellationToken cancellationToken)
        {
            try
            {
                return await session.TrySaveChangesAsync(cancellationToken)
                    ? NetworkOutcome.Succeeded
                    : NetworkOutcome.StaleVersion;
            }
            catch (DocumentAlreadyExistsException)
            {
                return await uniqueConflict();
            }
        }

        public async Task<NetworkResult> LoadSavedNetworkAsync(Guid networkId, CancellationToken cancellationToken)
        {
            var saved = await session.LoadAsync<NetworkDocument>(networkId, cancellationToken);
            return NetworkResult.Success(saved!);
        }
    }

    public static Task<NetworkOutcome> NameConflict() => Task.FromResult(NetworkOutcome.NameAlreadyUsed);
}
