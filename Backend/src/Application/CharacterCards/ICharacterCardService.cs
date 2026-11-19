namespace SciFiLarpNetwork.Backend.Application.CharacterCards;

/// <summary>Collects the data printed on character cards.</summary>
public interface ICharacterCardService
{
    /// <summary>The most cards one request can print.</summary>
    const int MaxCards = 200;

    /// <summary>
    /// Builds one card per requested identifier, in the requested order; a repeated identifier gives a repeated card.
    /// Reports <see cref="CharacterCardsOutcome.UnknownCharacters"/> when any identifier matches no character.
    /// </summary>
    Task<CharacterCardsResult> GetCardsAsync(IReadOnlyList<Guid> characterIds, CancellationToken cancellationToken = default);
}
