namespace SciFiLarpNetwork.Backend.Application.Players;

/// <summary>The talents a character picked and the skills it bought through them.</summary>
public interface ICharacterSkillService
{
    Task<CharacterSkillsSummary?> GetCharacterSkillsAsync(Guid characterId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Replaces the character's talents and skills if the character is still at <paramref name="expectedVersion"/>;
    /// otherwise reports <see cref="RosterOutcome.StaleVersion"/>. Every rule is checked on the lists being saved.
    /// </summary>
    Task<CharacterSkillsResult> SaveCharacterSkillsAsync(
        Guid characterId,
        long expectedVersion,
        CharacterSkillsDefinition definition,
        CancellationToken cancellationToken = default);
}
