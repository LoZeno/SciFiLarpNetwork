using JasperFx.Events;
using Marten;
using Marten.Pagination;
using SciFiLarpNetwork.Backend.Application.Players;
using SciFiLarpNetwork.Backend.Domain.Characters;
using SciFiLarpNetwork.Backend.Domain.Events.CharacterEvents;
using SciFiLarpNetwork.Backend.Domain.Events.PlayerEvents;
using SciFiLarpNetwork.Backend.Domain.Players;
using SciFiLarpNetwork.Backend.Domain.Skills;
using SciFiLarpNetwork.Backend.Infrastructure.Persistence.EventStore;
using SciFiLarpNetwork.Backend.Infrastructure.Players.PlayerDocuments;

namespace SciFiLarpNetwork.Backend.Infrastructure.Players;

/// <summary>
/// Player commands follow Marten's command handler workflow: <c>FetchForWriting</c> on every stream the decision
/// depends on, then one <c>SaveChangesAsync</c>. Character creation and activation append to the player's stream,
/// so a deactivation that runs at the same time conflicts on the player's version instead of missing a character.
/// </summary>
internal sealed class PlayerService(AttributedSessionFactory sessions, TimeProvider timeProvider) : IPlayerService
{
    public async Task<RosterPage<PlayerSummary>> GetPlayersAsync(RosterQuery query,
        CancellationToken cancellationToken = default)
    {
        await using var session = sessions.QuerySession();
        IQueryable<PlayerDocument> players = session.Query<PlayerDocument>();
        if (!query.IncludeArchived)
        {
            players = players.Where(player => !player.IsArchived);
        }

        var search = RosterSearch.From(query.Search);
        if (search is { NormalizedText: { } text, Number: { } number })
        {
            players = players.Where(player => player.NormalizedDisplayName.Contains(text) || player.Number == number);
        }
        else if (search.NormalizedText is { } nameOnly)
        {
            players = players.Where(player => player.NormalizedDisplayName.Contains(nameOnly));
        }

        var page = await players.OrderBy(player => player.Number)
            .ToPagedListAsync(query.PageNumber, RosterQuery.PageSize, cancellationToken);
        return RosterSearch.ToPage(page, page.Select(player => player.ToSummary()).ToArray());
    }

    public async Task<PlayerSummary?> GetPlayerAsync(Guid playerId, CancellationToken cancellationToken = default)
    {
        await using var session = sessions.QuerySession();
        return (await session.LoadAsync<PlayerDocument>(playerId, cancellationToken))?.ToSummary();
    }

    public Task<RosterResult<PlayerSummary>> CreatePlayerAsync(PlayerDefinition definition,
        CancellationToken cancellationToken = default) =>
        RosterWrites.RetryLostRacesAsync(() => CreatePlayerOnceAsync(definition, cancellationToken));

    private async Task<RosterResult<PlayerSummary>> CreatePlayerOnceAsync(PlayerDefinition definition,
        CancellationToken cancellationToken)
    {
        if (Validate(definition) is { } invalid)
        {
            return RosterResult<PlayerSummary>.Failure(invalid);
        }

        await using var session = sessions.OpenSession();
        var playerId = Guid.NewGuid();
        var sequence = await session.FetchRunningNumbersAsync(RunningNumberDocument.PlayerSequenceId, cancellationToken);
        var now = timeProvider.GetUtcNow();
        var number = sequence.IssueNext(playerId, now);
        session.Events.StartStream<PlayerDocument>(
            playerId,
            new PlayerRegistered(
                playerId,
                number,
                definition.DisplayName.Trim(),
                CatalogNames.Normalize(definition.DisplayName),
                PlayerContacts.Normalize(definition.Email),
                PlayerContacts.Normalize(definition.Phone),
                PlayerContacts.Normalize(definition.Notes),
                definition.Type,
                now));
        return await SaveAsync(session, playerId, editedVersion: null, cancellationToken);
    }

    public async Task<RosterResult<PlayerSummary>> UpdatePlayerAsync(
        Guid playerId,
        long expectedVersion,
        PlayerDefinition definition,
        CancellationToken cancellationToken = default)
    {
        if (Validate(definition) is { } invalid)
        {
            return RosterResult<PlayerSummary>.Failure(invalid);
        }

        await using var session = sessions.OpenSession();
        var (outcome, stream) =
            await session.FetchExpectedAsync<PlayerDocument>(playerId, expectedVersion, cancellationToken);
        if (stream?.Aggregate is not { } current)
        {
            return RosterResult<PlayerSummary>.Failure(outcome);
        }

        var changed = new PlayerDetailsChanged(
            playerId,
            definition.DisplayName.Trim(),
            CatalogNames.Normalize(definition.DisplayName),
            PlayerContacts.Normalize(definition.Email),
            PlayerContacts.Normalize(definition.Phone),
            PlayerContacts.Normalize(definition.Notes),
            definition.Type,
            timeProvider.GetUtcNow());
        if (changed.DisplayName == current.DisplayName &&
            changed.Email == current.Email &&
            changed.Phone == current.Phone &&
            changed.Notes == current.Notes &&
            changed.Type == current.Type)
        {
            return RosterResult<PlayerSummary>.Success(current.ToSummary());
        }

        stream.AppendOne(changed);
        return await SaveAsync(session, playerId, expectedVersion, cancellationToken);
    }

    public async Task<RosterResult<PlayerSummary>> DeactivatePlayerAsync(Guid playerId,
        CancellationToken cancellationToken = default)
    {
        await using var session = sessions.OpenSession();
        var stream = await session.Events.FetchForWriting<PlayerDocument>(playerId, cancellationToken);
        switch (stream.Aggregate)
        {
            case null:
                return RosterResult<PlayerSummary>.Failure(RosterOutcome.NotFound);
            case { Status: PlayerStatus.Inactive } current:
                return RosterResult<PlayerSummary>.Success(current.ToSummary());
        }

        var activeCharacters = await FetchActiveCharactersAsync(session, playerId, cancellationToken);
        var now = timeProvider.GetUtcNow();
        foreach (var character in activeCharacters)
        {
            character.AppendOne(new CharacterStatusChanged(character.Id, CharacterStatus.Retired, null, now));
        }

        stream.AppendOne(new PlayerDeactivated(playerId, now));
        return await SaveAsync(session, playerId, editedVersion: null, cancellationToken);
    }

    public async Task<RosterResult<PlayerSummary>> ActivatePlayerAsync(Guid playerId,
        CancellationToken cancellationToken = default)
    {
        await using var session = sessions.OpenSession();
        var stream = await session.Events.FetchForWriting<PlayerDocument>(playerId, cancellationToken);
        switch (stream.Aggregate)
        {
            case null:
                return RosterResult<PlayerSummary>.Failure(RosterOutcome.NotFound);
            case { Status: PlayerStatus.Active } current:
                return RosterResult<PlayerSummary>.Success(current.ToSummary());
            case { IsArchived: true }:
                return RosterResult<PlayerSummary>.Failure(RosterOutcome.Archived);
        }

        stream.AppendOne(new PlayerActivated(playerId, timeProvider.GetUtcNow()));
        return await SaveAsync(session, playerId, editedVersion: null, cancellationToken);
    }

    public async Task<RosterResult<PlayerSummary>> ArchivePlayerAsync(Guid playerId,
        CancellationToken cancellationToken = default)
    {
        await using var session = sessions.OpenSession();
        var stream = await session.Events.FetchForWriting<PlayerDocument>(playerId, cancellationToken);
        switch (stream.Aggregate)
        {
            case null:
                return RosterResult<PlayerSummary>.Failure(RosterOutcome.NotFound);
            case { IsArchived: true } current:
                return RosterResult<PlayerSummary>.Success(current.ToSummary());
            case { Status: not PlayerStatus.Inactive }:
                return RosterResult<PlayerSummary>.Failure(RosterOutcome.PlayerNotInactive);
        }

        stream.AppendOne(new PlayerArchived(playerId, timeProvider.GetUtcNow()));
        return await SaveAsync(session, playerId, editedVersion: null, cancellationToken);
    }

    public async Task<RosterResult<PlayerSummary>> UnarchivePlayerAsync(Guid playerId,
        CancellationToken cancellationToken = default)
    {
        await using var session = sessions.OpenSession();
        var stream = await session.Events.FetchForWriting<PlayerDocument>(playerId, cancellationToken);
        switch (stream.Aggregate)
        {
            case null:
                return RosterResult<PlayerSummary>.Failure(RosterOutcome.NotFound);
            case { IsArchived: false } current:
                return RosterResult<PlayerSummary>.Success(current.ToSummary());
        }

        stream.AppendOne(new PlayerUnarchived(playerId, timeProvider.GetUtcNow()));
        return await SaveAsync(session, playerId, editedVersion: null, cancellationToken);
    }

    private static RosterOutcome? Validate(PlayerDefinition definition) =>
        !CatalogNames.IsValid(definition.DisplayName) ? RosterOutcome.InvalidName
        : !PlayerContacts.IsValidEmail(definition.Email) ? RosterOutcome.InvalidEmail
        : !PlayerContacts.IsValidPhone(definition.Phone) ? RosterOutcome.InvalidPhone
        : !PlayerContacts.IsValidNotes(definition.Notes) ? RosterOutcome.InvalidNotes
        : !Enum.IsDefined(definition.Type) ? RosterOutcome.InvalidPlayerType
        : null;

    /// <summary>
    /// Takes every Active character of the player for writing, to retire them. A character that becomes Active or is
    /// created while this runs appends to the player's stream, so this command's version check on that stream
    /// rejects the commit.
    /// </summary>
    private static async Task<IReadOnlyList<IEventStream<CharacterDocument>>> FetchActiveCharactersAsync(
        IDocumentSession session,
        Guid playerId,
        CancellationToken cancellationToken)
    {
        var activeIds = await session.Query<CharacterDocument>()
            .Where(character => character.PlayerId == playerId && character.Status == CharacterStatus.Active)
            .Select(character => character.Id)
            .ToListAsync(cancellationToken);
        if (activeIds.Count == 0)
        {
            return [];
        }

        var streams = await session.Events.FetchManyForWriting<CharacterDocument>(activeIds.ToArray(), cancellationToken);
        return streams.Where(character => character.Aggregate is { Status: CharacterStatus.Active }).ToArray();
    }

    /// <summary>
    /// Commits the command. A commit rejected by a version check is <see cref="RosterOutcome.StaleVersion"/> only
    /// when the edited player itself moved on from <paramref name="editedVersion"/>; otherwise another command changed
    /// shared data first, which is <see cref="RosterOutcome.ConcurrentChange"/>.
    /// </summary>
    private async Task<RosterResult<PlayerSummary>> SaveAsync(
        IDocumentSession session,
        Guid playerId,
        long? editedVersion,
        CancellationToken cancellationToken)
    {
        var outcome = await session.CommitAsync(
            () => LostRaceOutcomeAsync(playerId, editedVersion, cancellationToken),
            RosterWrites.ConcurrentChange,
            cancellationToken);
        if (outcome != RosterOutcome.Succeeded)
        {
            return RosterResult<PlayerSummary>.Failure(outcome);
        }

        var saved = await session.LoadAsync<PlayerDocument>(playerId, cancellationToken);
        return RosterResult<PlayerSummary>.Success(saved!.ToSummary());
    }

    private async Task<RosterOutcome> LostRaceOutcomeAsync(Guid playerId, long? editedVersion,
        CancellationToken cancellationToken)
    {
        if (editedVersion is not { } version)
        {
            return RosterOutcome.ConcurrentChange;
        }

        await using var query = sessions.QuerySession();
        return await query.LoadAsync<PlayerDocument>(playerId, cancellationToken) is { } current &&
               current.Version == version
            ? RosterOutcome.ConcurrentChange
            : RosterOutcome.StaleVersion;
    }
}
