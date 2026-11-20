using SciFiLarpNetwork.Backend.Application.CharacterCards;

namespace SciFiLarpNetwork.Backend.Api.CharacterCards;

/// <summary>Maps card printing outcomes to stable Siren error codes. Each code is also its localization key.</summary>
public static class CharacterCardErrors
{
    public const string NoCharactersSelected = "errors.noCharactersSelected";
    public const string TooManyCharacters = "errors.tooManyCharacters";
    public const string UnknownCharacters = "errors.unknownCharacters";
    public const string CardRenderingFailed = "errors.cardRenderingFailed";

    /// <summary>The error detail listing the requested identifiers that match no character.</summary>
    public const string CharacterIdsErrorKey = "characterIds";

    public static IReadOnlyList<CharacterCard> Unwrap(CharacterCardsResult result) =>
        result.Outcome switch
        {
            CharacterCardsOutcome.Succeeded => result.Cards,
            CharacterCardsOutcome.NoCharacters => throw Create(StatusCodes.Status400BadRequest, NoCharactersSelected),
            CharacterCardsOutcome.TooManyCharacters => throw Create(StatusCodes.Status400BadRequest, TooManyCharacters),
            CharacterCardsOutcome.UnknownCharacters => throw new ApiException(
                StatusCodes.Status400BadRequest,
                UnknownCharacters,
                UnknownCharacters,
                new Dictionary<string, IReadOnlyList<string>>
                {
                    [CharacterIdsErrorKey] = result.UnknownCharacterIds.Select(id => id.ToString("D")).ToArray()
                }),
            _ => throw new ArgumentOutOfRangeException(nameof(result), result.Outcome, "Unknown card printing outcome.")
        };

    public static ApiException RenderingFailed() => Create(StatusCodes.Status500InternalServerError, CardRenderingFailed);

    private static ApiException Create(int statusCode, string code) => new(statusCode, code, code);
}
