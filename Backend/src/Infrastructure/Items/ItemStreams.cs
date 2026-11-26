using JasperFx;
using JasperFx.Events;
using Marten;
using SciFiLarpNetwork.Backend.Application.Items;

namespace SciFiLarpNetwork.Backend.Infrastructure.Items;

/// <summary>Command steps shared by the item services.</summary>
internal static class ItemStreams
{
    extension(IDocumentSession session)
    {
        /// <summary>
        /// Fetches a stream for writing at <paramref name="expectedVersion"/>. A missing stream is
        /// <see cref="ItemOutcome.NotFound"/>; a stream at another version is <see cref="ItemOutcome.StaleVersion"/>.
        /// </summary>
        public async Task<(ItemOutcome Outcome, IEventStream<TDocument>? Stream)> FetchExpectedAsync<TDocument>(
            Guid id,
            long expectedVersion,
            CancellationToken cancellationToken)
            where TDocument : class
        {
            try
            {
                var stream = await session.Events.FetchForWriting<TDocument>(id, expectedVersion, cancellationToken);
                return stream.Aggregate is null ? (ItemOutcome.NotFound, null) : (ItemOutcome.Succeeded, stream);
            }
            catch (ConcurrencyException)
            {
                return await session.LoadAsync<TDocument>(id, cancellationToken) is null
                    ? (ItemOutcome.NotFound, null)
                    : (ItemOutcome.StaleVersion, null);
            }
        }
    }
}
