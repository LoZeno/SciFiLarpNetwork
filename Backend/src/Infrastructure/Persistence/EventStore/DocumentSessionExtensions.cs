using JasperFx;
using Marten;

namespace SciFiLarpNetwork.Backend.Infrastructure.Persistence.EventStore;

internal static class DocumentSessionExtensions
{
    extension(IDocumentSession session)
    {
        /// <summary>
        /// Commits the session and reports <see langword="false"/> when Marten rejects it with a
        /// <see cref="ConcurrencyException"/>; in that case nothing was written.
        /// </summary>
        public async Task<bool> TrySaveChangesAsync(CancellationToken cancellationToken)
        {
            try
            {
                await session.SaveChangesAsync(cancellationToken);
                return true;
            }
            catch (ConcurrencyException)
            {
                return false;
            }
        }
    }
}
