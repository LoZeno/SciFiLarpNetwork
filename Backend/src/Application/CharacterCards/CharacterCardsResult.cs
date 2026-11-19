namespace SciFiLarpNetwork.Backend.Application.CharacterCards;

/// <param name="Cards">The cards, in the requested order, when the outcome is <see cref="CharacterCardsOutcome.Succeeded"/>.</param>
/// <param name="UnknownCharacterIds">The requested identifiers that match no character.</param>
public sealed record CharacterCardsResult(
    CharacterCardsOutcome Outcome,
    IReadOnlyList<CharacterCard> Cards,
    IReadOnlyList<Guid> UnknownCharacterIds);
