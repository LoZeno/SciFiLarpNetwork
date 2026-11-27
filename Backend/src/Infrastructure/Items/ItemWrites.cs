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
            var outcome = await session.CommitItemAsync(sessions, itemId, editedVersion, uniqueConflict, cancellationToken);
            return outcome == ItemOutcome.Succeeded
                ? ItemResult<ItemSummary>.Success(await session.ReadBackItemAsync(itemId))
                : ItemResult<ItemSummary>.Failure(outcome);
        }

        /// <summary>
        /// Commits the command; <see cref="ItemOutcome.Succeeded"/> means it committed. See
        /// <see cref="SaveItemAsync"/> for what a rejected commit means.
        /// </summary>
        public async Task<ItemOutcome> CommitItemAsync(
            AttributedSessionFactory sessions,
            Guid itemId,
            long? editedVersion,
            Func<Task<ItemOutcome>> uniqueConflict,
            CancellationToken cancellationToken)
        {
            try
            {
                return await session.TrySaveChangesAsync(cancellationToken)
                    ? ItemOutcome.Succeeded
                    : await LostRaceOutcomeAsync(sessions, itemId, editedVersion, cancellationToken);
            }
            catch (ExistingStreamIdCollisionException)
            {
                // Two first-ever component additions both started the item-composition stream; the later one lost.
                return ItemOutcome.ConcurrentChange;
            }
            catch (DocumentAlreadyExistsException)
            {
                return await uniqueConflict();
            }
        }

        /// <summary>The committed item. Once committed, a cancelled read-back must not look like a failed command.</summary>
        public async Task<ItemSummary> ReadBackItemAsync(Guid itemId)
        {
            var saved = await session.LoadAsync<ItemDocument>(itemId, CancellationToken.None);
            return await session.SummarizeAsync(saved!, CancellationToken.None);
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
