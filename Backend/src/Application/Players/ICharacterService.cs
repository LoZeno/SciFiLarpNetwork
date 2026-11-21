namespace SciFiLarpNetwork.Backend.Application.Players;

/// <summary>Characters: the fictional personas players control.</summary>
public interface ICharacterService
{
    Task<RosterPage<CharacterSummary>> SearchCharactersAsync(RosterQuery query,
        CancellationToken cancellationToken = default);

    /// <summary>Every character of the player, ordered by running number.</summary>
    Task<IReadOnlyList<CharacterSummary>> GetCharactersOfPlayerAsync(
        Guid playerId,
        bool includeArchived,
        CancellationToken cancellationToken = default);

    /// <summary>One page of the player's characters, ordered by running number.</summary>
    Task<RosterPage<CharacterSummary>> GetCharacterPageOfPlayerAsync(
        Guid playerId,
        bool includeArchived,
        int pageNumber,
        CancellationToken cancellationToken = default);

    Task<CharacterSummary?> GetCharacterAsync(Guid characterId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Finds the character with the QR identifier: <see cref="RosterOutcome.InvalidQrIdentifier"/> when the text is
    /// not an identifier, and <see cref="RosterOutcome.NotFound"/> when no character has it.
    /// </summary>
    Task<RosterResult<CharacterSummary>> FindByQrIdentifierAsync(string identifier,
        CancellationToken cancellationToken = default);

    /// <summary>Creates an Active character for an Active player, with the next character running number.</summary>
    Task<RosterResult<CharacterSummary>> CreateCharacterAsync(
        Guid playerId,
        CharacterDefinition definition,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Replaces the character's details, status, network, and listed stat values if the character is still at
    /// <paramref name="expectedVersion"/>; otherwise reports <see cref="RosterOutcome.StaleVersion"/>.
    /// </summary>
    Task<RosterResult<CharacterSummary>> UpdateCharacterAsync(
        Guid characterId,
        long expectedVersion,
        CharacterDefinition definition,
        CancellationToken cancellationToken = default);

    /// <summary>Gives the character a new QR identifier; the old one no longer resolves.</summary>
    Task<RosterResult<CharacterSummary>> RegenerateQrIdentifierAsync(Guid characterId,
        CancellationToken cancellationToken = default);

    /// <summary>Archives a Retired or Dead character.</summary>
    Task<RosterResult<CharacterSummary>> ArchiveCharacterAsync(Guid characterId,
        CancellationToken cancellationToken = default);

    Task<RosterResult<CharacterSummary>> UnarchiveCharacterAsync(Guid characterId,
        CancellationToken cancellationToken = default);
}
