using JasperFx.Events;
using Marten;
using Marten.Pagination;
using SciFiLarpNetwork.Backend.Application.Players;
using SciFiLarpNetwork.Backend.Domain.Characters;
using SciFiLarpNetwork.Backend.Domain.CharacterSkills;
using SciFiLarpNetwork.Backend.Domain.Events.CharacterEvents;
using SciFiLarpNetwork.Backend.Domain.Events.CharacterStatEvents;
using SciFiLarpNetwork.Backend.Domain.Events.PlayerEvents;
using SciFiLarpNetwork.Backend.Domain.Players;
using SciFiLarpNetwork.Backend.Domain.Skills;
using SciFiLarpNetwork.Backend.Infrastructure.Networks.NetworkDocuments;
using SciFiLarpNetwork.Backend.Infrastructure.Persistence.EventStore;
using SciFiLarpNetwork.Backend.Infrastructure.Players.PlayerDocuments;
using SciFiLarpNetwork.Backend.Infrastructure.Skills.SkillDocuments;

namespace SciFiLarpNetwork.Backend.Infrastructure.Players;

/// <summary>
/// Character commands follow Marten's command handler workflow: <c>FetchForWriting</c> on every stream the
/// decision depends on, then one <c>SaveChangesAsync</c>. Creating a character or making it Active appends to the
/// player's stream, so it conflicts with a concurrent deactivation of the player; a creation that loses such a race
/// is run again, and then sees the Inactive player. A newly chosen network's stream is
/// marked <c>AlwaysEnforceConsistency</c>; a network disabled or made default at the very moment of the commit can
/// still be kept, which the specification allows for networks that change after they were chosen.
/// </summary>
internal sealed class CharacterService(AttributedSessionFactory sessions, TimeProvider timeProvider) : ICharacterService
{
    public async Task<RosterPage<CharacterSummary>> SearchCharactersAsync(RosterQuery query,
        CancellationToken cancellationToken = default)
    {
        await using var session = sessions.QuerySession();
        IQueryable<CharacterDocument> characters = session.Query<CharacterDocument>();
        if (!query.IncludeArchived)
        {
            characters = characters.Where(character => !character.IsArchived);
        }

        var search = RosterSearch.From(query.Search);
        if (search is { NormalizedText: { } text, Number: { } number })
        {
            characters = characters.Where(character =>
                character.NormalizedName.Contains(text) || character.Number == number);
        }
        else if (search.NormalizedText is { } nameOnly)
        {
            characters = characters.Where(character => character.NormalizedName.Contains(nameOnly));
        }

        var page = await characters.OrderBy(character => character.Number)
            .ToPagedListAsync(query.PageNumber, RosterQuery.PageSize, cancellationToken);
        return RosterSearch.ToPage(page, await ToSummariesAsync(session, page.ToArray(), cancellationToken));
    }

    public async Task<RosterPage<CharacterSummary>> GetCharacterPageOfPlayerAsync(
        Guid playerId,
        bool includeArchived,
        int pageNumber,
        CancellationToken cancellationToken = default)
    {
        await using var session = sessions.QuerySession();
        var characters = session.Query<CharacterDocument>().Where(character => character.PlayerId == playerId);
        if (!includeArchived)
        {
            characters = characters.Where(character => !character.IsArchived);
        }

        var page = await characters.OrderBy(character => character.Number)
            .ToPagedListAsync(pageNumber, RosterQuery.PageSize, cancellationToken);
        return RosterSearch.ToPage(page, await ToSummariesAsync(session, page.ToArray(), cancellationToken));
    }

    public async Task<CharacterSummary?> GetCharacterAsync(Guid characterId,
        CancellationToken cancellationToken = default)
    {
        await using var session = sessions.QuerySession();
        return await session.LoadAsync<CharacterDocument>(characterId, cancellationToken) is { } character
            ? (await ToSummariesAsync(session, [character], cancellationToken))[0]
            : null;
    }

    public async Task<RosterResult<CharacterSummary>> FindByQrIdentifierAsync(string identifier,
        CancellationToken cancellationToken = default)
    {
        if (!QrIdentifiers.TryNormalize(identifier, out var normalized))
        {
            return RosterResult<CharacterSummary>.Failure(RosterOutcome.InvalidQrIdentifier);
        }

        await using var session = sessions.QuerySession();
        var character = await session.Query<CharacterDocument>()
            .Where(character => character.QrIdentifier == normalized)
            .FirstOrDefaultAsync(cancellationToken);
        return character is null
            ? RosterResult<CharacterSummary>.Failure(RosterOutcome.NotFound)
            : RosterResult<CharacterSummary>.Success((await ToSummariesAsync(session, [character], cancellationToken))[0]);
    }

    public Task<RosterResult<CharacterSummary>> CreateCharacterAsync(
        Guid playerId,
        CharacterDefinition definition,
        CancellationToken cancellationToken = default) =>
        RosterWrites.RetryLostRacesAsync(() => CreateCharacterOnceAsync(playerId, definition, cancellationToken));

    private async Task<RosterResult<CharacterSummary>> CreateCharacterOnceAsync(
        Guid playerId,
        CharacterDefinition definition,
        CancellationToken cancellationToken)
    {
        if (CharacterDefinitionValidator.ValidateCreation(definition) is { } invalid)
        {
            return RosterResult<CharacterSummary>.Failure(invalid);
        }

        await using var session = sessions.OpenSession();
        var player = await session.Events.FetchForWriting<PlayerDocument>(playerId, cancellationToken);
        switch (player.Aggregate)
        {
            case null:
                return RosterResult<CharacterSummary>.Failure(RosterOutcome.NotFound);
            case { Status: PlayerStatus.Inactive }:
                return RosterResult<CharacterSummary>.Failure(RosterOutcome.PlayerInactive);
        }

        if (definition.NetworkId is { } networkId &&
            await LockSelectableNetworkAsync(session, networkId, cancellationToken) is var networkOutcome and not
                RosterOutcome.Succeeded)
        {
            return RosterResult<CharacterSummary>.Failure(networkOutcome);
        }

        var sequence = await session.FetchRunningNumbersAsync(RunningNumberDocument.CharacterSequenceId, cancellationToken);
        var now = timeProvider.GetUtcNow();
        var (statOutcome, statValues) = await ResolveStatValuesAsync(session, definition, now, cancellationToken);
        if (statOutcome != RosterOutcome.Succeeded)
        {
            return RosterResult<CharacterSummary>.Failure(statOutcome);
        }

        var characterId = Guid.NewGuid();
        var number = sequence.IssueNext(characterId, now);
        player.AppendOne(new PlayerCharacterActivated(playerId, characterId, now));
        session.Events.StartStream<CharacterDocument>(
            characterId,
            new CharacterCreated(
                characterId,
                number,
                playerId,
                definition.Name.Trim(),
                CatalogNames.Normalize(definition.Name),
                CharacterTexts.Normalize(definition.Title),
                CharacterTexts.Normalize(definition.Species),
                CharacterTexts.Normalize(definition.Gender),
                CharacterTexts.Normalize(definition.Type),
                definition.ExperiencePoints,
                definition.NetworkId,
                QrIdentifiers.Create(),
                statValues.Select(resolved => resolved.Value).ToArray(),
                now));
        return await SaveAsync(session, characterId, editedVersion: null, definition, cancellationToken);
    }

    public async Task<RosterResult<CharacterSummary>> UpdateCharacterAsync(
        Guid characterId,
        long expectedVersion,
        CharacterDefinition definition,
        CancellationToken cancellationToken = default)
    {
        if (CharacterDefinitionValidator.Validate(definition) is { } invalid)
        {
            return RosterResult<CharacterSummary>.Failure(invalid);
        }

        await using var session = sessions.OpenSession();
        var (fetchOutcome, stream) =
            await session.FetchExpectedAsync<CharacterDocument>(characterId, expectedVersion, cancellationToken);
        if (stream?.Aggregate is not { } current)
        {
            return RosterResult<CharacterSummary>.Failure(fetchOutcome);
        }

        if (await LeavesNegativeFreePointsAsync(session, current, definition.ExperiencePoints, cancellationToken))
        {
            return RosterResult<CharacterSummary>.Failure(RosterOutcome.InsufficientFreePoints);
        }

        IEventStream<PlayerDocument>? activatingPlayer = null;
        if (definition.Status == CharacterStatus.Active && current.Status != CharacterStatus.Active)
        {
            (var activationOutcome, activatingPlayer) = await FetchActivePlayerAsync(session, current, cancellationToken);
            if (activationOutcome != RosterOutcome.Succeeded)
            {
                return RosterResult<CharacterSummary>.Failure(activationOutcome);
            }
        }

        var networkChanged = definition.NetworkId != current.NetworkId;
        if (networkChanged && definition.NetworkId is { } networkId &&
            await LockSelectableNetworkAsync(session, networkId, cancellationToken) is var networkOutcome and not
                RosterOutcome.Succeeded)
        {
            return RosterResult<CharacterSummary>.Failure(networkOutcome);
        }

        var now = timeProvider.GetUtcNow();
        activatingPlayer?.AppendOne(new PlayerCharacterActivated(current.PlayerId, characterId, now));
        if (networkChanged)
        {
            stream.AppendOne(new CharacterNetworkChanged(characterId, definition.NetworkId, now));
        }

        var (statOutcome, statValues) = await ResolveStatValuesAsync(session, definition, now, cancellationToken);
        if (statOutcome != RosterOutcome.Succeeded)
        {
            return RosterResult<CharacterSummary>.Failure(statOutcome);
        }

        AppendChanges(stream, current, definition, statValues, now);
        return await SaveAsync(session, characterId, expectedVersion, definition, cancellationToken);
    }

    public async Task<RosterResult<CharacterSummary>> RegenerateQrIdentifierAsync(Guid characterId,
        CancellationToken cancellationToken = default)
    {
        await using var session = sessions.OpenSession();
        var stream = await session.Events.FetchForWriting<CharacterDocument>(characterId, cancellationToken);
        if (stream.Aggregate is null)
        {
            return RosterResult<CharacterSummary>.Failure(RosterOutcome.NotFound);
        }

        stream.AppendOne(new CharacterQrIdentifierRegenerated(characterId, QrIdentifiers.Create(), timeProvider.GetUtcNow()));
        return await SaveAsync(session, characterId, editedVersion: null, definition: null, cancellationToken);
    }

    public async Task<RosterResult<CharacterSummary>> ArchiveCharacterAsync(Guid characterId,
        CancellationToken cancellationToken = default)
    {
        await using var session = sessions.OpenSession();
        var stream = await session.Events.FetchForWriting<CharacterDocument>(characterId, cancellationToken);
        switch (stream.Aggregate)
        {
            case null:
                return RosterResult<CharacterSummary>.Failure(RosterOutcome.NotFound);
            case { IsArchived: true } current:
                return RosterResult<CharacterSummary>.Success(await ToSummaryAsync(session, current, cancellationToken));
            case { Status: CharacterStatus.Active }:
                return RosterResult<CharacterSummary>.Failure(RosterOutcome.CharacterNotArchivable);
        }

        stream.AppendOne(new CharacterArchived(characterId, timeProvider.GetUtcNow()));
        return await SaveAsync(session, characterId, editedVersion: null, definition: null, cancellationToken);
    }

    public async Task<RosterResult<CharacterSummary>> UnarchiveCharacterAsync(Guid characterId,
        CancellationToken cancellationToken = default)
    {
        await using var session = sessions.OpenSession();
        var stream = await session.Events.FetchForWriting<CharacterDocument>(characterId, cancellationToken);
        switch (stream.Aggregate)
        {
            case null:
                return RosterResult<CharacterSummary>.Failure(RosterOutcome.NotFound);
            case { IsArchived: false } current:
                return RosterResult<CharacterSummary>.Success(await ToSummaryAsync(session, current, cancellationToken));
        }

        stream.AppendOne(new CharacterUnarchived(characterId, timeProvider.GetUtcNow()));
        return await SaveAsync(session, characterId, editedVersion: null, definition: null, cancellationToken);
    }

    /// <summary>
    /// Checks that an archived character is not made Active and that its player is Active, and takes the player's
    /// stream for writing: the activation is recorded there, so a concurrent deactivation of the player conflicts
    /// with this command.
    /// </summary>
    private static async Task<(RosterOutcome Outcome, IEventStream<PlayerDocument>? Player)> FetchActivePlayerAsync(
        IDocumentSession session,
        CharacterDocument character,
        CancellationToken cancellationToken)
    {
        if (character.IsArchived)
        {
            return (RosterOutcome.Archived, null);
        }

        var player = await session.Events.FetchForWriting<PlayerDocument>(character.PlayerId, cancellationToken);
        return player.Aggregate is { Status: PlayerStatus.Active }
            ? (RosterOutcome.Succeeded, player)
            : (RosterOutcome.PlayerInactive, null);
    }

    /// <summary>
    /// Whether lowering the experience points would make free points negative. Free points that are already negative
    /// because a skill's cost was raised do not block other edits, or raising the experience points.
    /// </summary>
    private static async Task<bool> LeavesNegativeFreePointsAsync(
        IQuerySession session,
        CharacterDocument character,
        int experiencePoints,
        CancellationToken cancellationToken)
    {
        if (experiencePoints >= character.ExperiencePoints || character.SkillPurchases.Length == 0)
        {
            return false;
        }

        var skillIds = character.SkillPurchases.Select(purchase => purchase.SkillId).ToArray();
        var costs = (await session.LoadManyAsync<SkillDocument>(cancellationToken, skillIds))
            .ToDictionary(skill => skill.Id, skill => skill.Cost);
        return CharacterSkillRules.FreePoints(experiencePoints, character.SkillPurchases, skillId => costs[skillId]) < 0;
    }

    private static async Task<RosterOutcome> LockSelectableNetworkAsync(
        IDocumentSession session,
        Guid networkId,
        CancellationToken cancellationToken)
    {
        var network = await session.Events.FetchForWriting<NetworkDocument>(networkId, cancellationToken);
        switch (network.Aggregate)
        {
            case null:
                return RosterOutcome.UnknownNetwork;
            case { IsEnabled: false }:
                return RosterOutcome.NetworkDisabled;
            case { IsDefault: true }:
                return RosterOutcome.DefaultNetworkNotSelectable;
            default:
                network.AlwaysEnforceConsistency = true;
                return RosterOutcome.Succeeded;
        }
    }

    /// <summary>
    /// Checks the values for existing stats against their types, and defines each newly typed stat name that does
    /// not exist yet, with the typed value as its default for every character. A typed name that already exists uses
    /// that stat, which must have the same type; its default does not change.
    /// </summary>
    private static async Task<(RosterOutcome Outcome, IReadOnlyList<ResolvedStatValue> Values)> ResolveStatValuesAsync(
        IDocumentSession session,
        CharacterDefinition definition,
        DateTimeOffset now,
        CancellationToken cancellationToken)
    {
        var statIds = definition.StatValues.Select(input => input.StatId).Distinct().ToArray();
        var stats = statIds.Length == 0
            ? []
            : await session.LoadManyAsync<CharacterStatDocument>(cancellationToken, statIds);
        if (stats.Count != statIds.Length)
        {
            return (RosterOutcome.UnknownStat, []);
        }

        var statsById = stats.ToDictionary(stat => stat.Id);
        if (definition.StatValues.Any(input => !CharacterStatValues.IsValid(statsById[input.StatId].Type, input.Number, input.Text)))
        {
            return (RosterOutcome.InvalidStatValue, []);
        }

        if (CharacterDefinitionValidator.ValidateNewStats(definition.NewStats) is { } invalid)
        {
            return (invalid, []);
        }

        var resolved = definition.StatValues
            .Select(input => new ResolvedStatValue(
                CharacterStatValues.Normalize(input.StatId, input.Number, input.Text),
                statsById[input.StatId].Type,
                statsById[input.StatId].DefaultValue()))
            .ToList();
        foreach (var newStat in definition.NewStats.DistinctBy(stat => CatalogNames.Normalize(stat.Name)))
        {
            var normalizedName = CatalogNames.Normalize(newStat.Name);
            var existing = await session.Query<CharacterStatDocument>()
                .Where(stat => stat.NormalizedName == normalizedName)
                .FirstOrDefaultAsync(cancellationToken);
            if (existing is not null && existing.Type != newStat.Type)
            {
                return (RosterOutcome.NameAlreadyUsed, []);
            }

            if (existing is { IsArchived: true })
            {
                return (RosterOutcome.StatArchived, []);
            }

            var statId = existing?.Id ?? Guid.NewGuid();
            var value = CharacterStatValues.Normalize(statId, newStat.Number, newStat.Text);
            if (existing is null)
            {
                session.Events.StartStream<CharacterStatDocument>(statId,
                    new CharacterStatDefined(statId, newStat.Name.Trim(), normalizedName, newStat.Type, value.Number,
                        value.Text, now));
            }

            resolved.Add(new ResolvedStatValue(value, newStat.Type, existing?.DefaultValue() ?? value));
        }

        return (RosterOutcome.Succeeded, resolved.DistinctBy(value => value.Value.StatId).ToArray());
    }

    private static void AppendChanges(
        IEventStream<CharacterDocument> stream,
        CharacterDocument current,
        CharacterDefinition definition,
        IReadOnlyList<ResolvedStatValue> statValues,
        DateTimeOffset now)
    {
        var details = new CharacterDetailsChanged(
            current.Id,
            definition.Name.Trim(),
            CatalogNames.Normalize(definition.Name),
            CharacterTexts.Normalize(definition.Title),
            CharacterTexts.Normalize(definition.Species),
            CharacterTexts.Normalize(definition.Gender),
            CharacterTexts.Normalize(definition.Type),
            definition.ExperiencePoints,
            now);
        if (details.Name != current.Name ||
            details.Title != current.Title ||
            details.Species != current.Species ||
            details.Gender != current.Gender ||
            details.Type != current.Type ||
            details.ExperiencePoints != current.ExperiencePoints)
        {
            stream.AppendOne(details);
        }

        var deathDate = definition.Status == CharacterStatus.Dead ? definition.DeathDate : null;
        if (definition.Status != current.Status || deathDate != current.DeathDate)
        {
            stream.AppendOne(new CharacterStatusChanged(current.Id, definition.Status, deathDate, now));
        }

        var changedValues = statValues
            .Where(value => !SameValue(current, value))
            .Select(value => value.Value)
            .ToArray();
        if (changedValues.Length > 0)
        {
            stream.AppendOne(new CharacterStatValuesChanged(current.Id, changedValues, now));
        }
    }

    /// <summary>Whether the character already has the requested value, either its own or the stat's default.</summary>
    private static bool SameValue(CharacterDocument character, ResolvedStatValue requested)
    {
        var current = character.StatValues.FirstOrDefault(value => value.StatId == requested.Value.StatId)
                      ?? requested.Default;
        return requested.Type == CharacterStatType.Number
            ? current.Number == requested.Value.Number
            : current.Text == requested.Value.Text;
    }

    /// <summary>
    /// Commits the command. A version-check rejection is <see cref="RosterOutcome.StaleVersion"/> only when the edited
    /// character itself moved on from <paramref name="editedVersion"/>. A unique-index rejection is
    /// <see cref="RosterOutcome.NameAlreadyUsed"/> when a typed stat name was defined by another command first, and
    /// otherwise a concurrent change.
    /// </summary>
    private async Task<RosterResult<CharacterSummary>> SaveAsync(
        IDocumentSession session,
        Guid characterId,
        long? editedVersion,
        CharacterDefinition? definition,
        CancellationToken cancellationToken)
    {
        var outcome = await session.CommitAsync(
            () => LostRaceOutcomeAsync(characterId, editedVersion, cancellationToken),
            () => UniqueConflictOutcomeAsync(definition, cancellationToken),
            cancellationToken);
        if (outcome != RosterOutcome.Succeeded)
        {
            return RosterResult<CharacterSummary>.Failure(outcome);
        }

        var saved = await session.LoadAsync<CharacterDocument>(characterId, cancellationToken);
        return RosterResult<CharacterSummary>.Success(await ToSummaryAsync(session, saved!, cancellationToken));
    }

    private async Task<RosterOutcome> LostRaceOutcomeAsync(Guid characterId, long? editedVersion,
        CancellationToken cancellationToken)
    {
        if (editedVersion is not { } version)
        {
            return RosterOutcome.ConcurrentChange;
        }

        await using var query = sessions.QuerySession();
        return await query.LoadAsync<CharacterDocument>(characterId, cancellationToken) is { } current &&
               current.Version == version
            ? RosterOutcome.ConcurrentChange
            : RosterOutcome.StaleVersion;
    }

    private async Task<RosterOutcome> UniqueConflictOutcomeAsync(CharacterDefinition? definition,
        CancellationToken cancellationToken)
    {
        var typedNames = definition?.NewStats.Select(stat => CatalogNames.Normalize(stat.Name)).ToArray() ?? [];
        if (typedNames.Length == 0)
        {
            return RosterOutcome.ConcurrentChange;
        }

        await using var query = sessions.QuerySession();
        return await query.Query<CharacterStatDocument>().AnyAsync(stat => typedNames.Contains(stat.NormalizedName),
            cancellationToken)
            ? RosterOutcome.NameAlreadyUsed
            : RosterOutcome.ConcurrentChange;
    }

    private static async Task<CharacterSummary> ToSummaryAsync(IQuerySession session, CharacterDocument character,
        CancellationToken cancellationToken) =>
        (await ToSummariesAsync(session, [character], cancellationToken))[0];

    private static async Task<IReadOnlyList<CharacterSummary>> ToSummariesAsync(
        IQuerySession session,
        CharacterDocument[] characters,
        CancellationToken cancellationToken)
    {
        if (characters.Length == 0)
        {
            return [];
        }

        var playerIds = characters.Select(character => character.PlayerId).Distinct().ToArray();
        var players = (await session.LoadManyAsync<PlayerDocument>(cancellationToken, playerIds))
            .ToDictionary(player => player.Id, player => player.DisplayName);
        var stats = (await session.Query<CharacterStatDocument>().ToListAsync(cancellationToken))
            .OrderBy(stat => stat.NormalizedName, StringComparer.Ordinal)
            .ToArray();
        return characters
            .Select(character => character.ToSummary(players.GetValueOrDefault(character.PlayerId, string.Empty), stats))
            .ToArray();
    }

    /// <param name="Default">The value of a character that has no value of its own for the stat.</param>
    private sealed record ResolvedStatValue(CharacterStatValue Value, CharacterStatType Type, CharacterStatValue Default);
}
