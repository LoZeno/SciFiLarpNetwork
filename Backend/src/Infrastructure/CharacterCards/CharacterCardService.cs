using SciFiLarpNetwork.Backend.Application.CharacterCards;
using SciFiLarpNetwork.Backend.Application.Players;
using SciFiLarpNetwork.Backend.Application.Skills;

namespace SciFiLarpNetwork.Backend.Infrastructure.CharacterCards;

internal sealed class CharacterCardService(
    IPlayerService players,
    ICharacterService characters,
    ICharacterSkillService characterSkills,
    ISkillService skills,
    ICardFlagService cardFlags) : ICharacterCardService
{
    public async Task<CharacterCardsResult> GetCardsAsync(IReadOnlyList<Guid> characterIds,
        CancellationToken cancellationToken = default)
    {
        if (characterIds.Count == 0)
        {
            return Failure(CharacterCardsOutcome.NoCharacters);
        }

        if (characterIds.Count > ICharacterCardService.MaxCards)
        {
            return Failure(CharacterCardsOutcome.TooManyCharacters);
        }

        var skillCatalog = await skills.GetSkillsAsync(null, includeArchived: true, cancellationToken);
        var cardFlagCatalog = await cardFlags.GetCardFlagsAsync(cancellationToken);
        var cards = new Dictionary<Guid, CharacterCard>();
        var unknown = new List<Guid>();
        foreach (var characterId in characterIds.Distinct())
        {
            if (await ComposeAsync(characterId, skillCatalog, cardFlagCatalog, cancellationToken) is { } card)
            {
                cards[characterId] = card;
            }
            else
            {
                unknown.Add(characterId);
            }
        }

        return unknown.Count > 0
            ? new CharacterCardsResult(CharacterCardsOutcome.UnknownCharacters, [], unknown)
            : new CharacterCardsResult(CharacterCardsOutcome.Succeeded, characterIds.Select(id => cards[id]).ToArray(), []);
    }

    private async Task<CharacterCard?> ComposeAsync(
        Guid characterId,
        IReadOnlyList<SkillSummary> skillCatalog,
        IReadOnlyList<CardFlagSummary> cardFlagCatalog,
        CancellationToken cancellationToken)
    {
        if (await characters.GetCharacterAsync(characterId, cancellationToken) is not { } character ||
            await players.GetPlayerAsync(character.PlayerId, cancellationToken) is not { } player ||
            await characterSkills.GetCharacterSkillsAsync(characterId, cancellationToken) is not { } owned)
        {
            return null;
        }

        return CharacterCardComposer.Compose(player, character, owned, skillCatalog, cardFlagCatalog);
    }

    private static CharacterCardsResult Failure(CharacterCardsOutcome outcome) => new(outcome, [], []);
}
