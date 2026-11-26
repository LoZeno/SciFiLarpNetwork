using JasperFx;
using Marten;
using SciFiLarpNetwork.Backend.Application.Items;
using SciFiLarpNetwork.Backend.Infrastructure.Items.ItemDocuments;
using SciFiLarpNetwork.Backend.Infrastructure.Persistence.EventStore;
using ExistingStreamIdCollisionException = Marten.Exceptions.ExistingStreamIdCollisionException;

namespace SciFiLarpNetwork.Backend.Infrastructure.Items;

/// <summary>Commits item commands and decides what a rejected commit means.</summary>
internal static class ItemWrites
{
    extension(IDocumentSession session)
    {
        /// <summary>
        /// Commits the command and returns the saved item. A commit rejected by a version check is
        /// <see cref="ItemOutcome.StaleVersion"/> only when the item itself moved on from <paramref name="editedVersion"/>;
        /// otherwise another command changed shared data first, which is <see cref="ItemOutcome.ConcurrentChange"/>.
        /// <paramref name="uniqueConflict"/> decides which unique value (name or recipe) a concurrent command claimed
        /// first when a unique index rejects the commit.
        /// </summary>
        public async Task<ItemResult<ItemSummary>> SaveItemAsync(
            AttributedSessionFactory sessions,
            Guid itemId,
            long? editedVersion,
            Func<Task<ItemOutcome>> uniqueConflict,
            CancellationToken cancellationToken)
        {
            try
            {
                if (!await session.TrySaveChangesAsync(cancellationToken))
                {
                    return ItemResult<ItemSummary>.Failure(await LostRaceOutcomeAsync(sessions, itemId, editedVersion, cancellationToken));
                }
            }
            catch (ExistingStreamIdCollisionException)
            {
                // Two first-ever component additions both started the item-composition stream; the later one lost.
                return ItemResult<ItemSummary>.Failure(ItemOutcome.ConcurrentChange);
            }
            catch (DocumentAlreadyExistsException)
            {
                return ItemResult<ItemSummary>.Failure(await uniqueConflict());
            }

            // Once committed, a cancelled read-back must not look like a failed command.
            var saved = await session.LoadAsync<ItemDocument>(itemId, CancellationToken.None);
            return ItemResult<ItemSummary>.Success(await session.SummarizeAsync(saved!, CancellationToken.None));
        }
    }

    /// <summary>
    /// After a unique index rejected a commit: the name, when another item now has it; otherwise the recipe.
    /// </summary>
    public static Func<Task<ItemOutcome>> NameOrRecipeConflict(
        AttributedSessionFactory sessions, Guid itemId, string normalizedName) =>
        async () =>
        {
            await using var query = sessions.QuerySession();
            return await query.Query<ItemDocument>()
                .AnyAsync(item => item.NormalizedName == normalizedName && item.Id != itemId)
                ? ItemOutcome.NameAlreadyUsed
                : ItemOutcome.DuplicateRecipe;
        };

    private static async Task<ItemOutcome> LostRaceOutcomeAsync(
        AttributedSessionFactory sessions, Guid itemId, long? editedVersion, CancellationToken cancellationToken)
    {
        if (editedVersion is not { } version)
        {
            return ItemOutcome.ConcurrentChange;
        }

        await using var query = sessions.QuerySession();
        return await query.LoadAsync<ItemDocument>(itemId, cancellationToken) is { } current && current.Version == version
            ? ItemOutcome.ConcurrentChange
            : ItemOutcome.StaleVersion;
    }
}
