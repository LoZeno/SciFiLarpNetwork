using SciFiLarpNetwork.Backend.Api.Players;
using SciFiLarpNetwork.Backend.Api.Skills;
using SciFiLarpNetwork.Backend.Application.News;

namespace SciFiLarpNetwork.Backend.Api.News;

/// <summary>Maps news outcomes to stable Siren error codes. Each code is also its localization key.</summary>
public static class NewsErrors
{
    public const string InvalidStoryId = "errors.invalidStoryId";
    public const string InvalidNewsTitle = "errors.invalidNewsTitle";
    public const string InvalidNewsBody = "errors.invalidNewsBody";
    public const string InvalidNewsTimes = "errors.invalidNewsTimes";
    public const string InvalidHackingDifficulty = "errors.invalidHackingDifficulty";
    public const string UnknownAuthor = "errors.unknownAuthor";
    public const string AuthorArchived = "errors.authorArchived";
    public const string ExternalImage = "errors.externalImage";
    public const string MissingMediaText = "errors.missingMediaText";
    public const string InvalidMediaReference = "errors.invalidMediaReference";
    public const string TooManyMedia = "errors.tooManyMedia";
    public const string UnknownMedia = "errors.unknownMedia";
    public const string OptionsOnPicture = "errors.optionsOnPicture";
    public const string StoryIdInUse = "errors.storyIdInUse";
    public const string InvalidMediaName = "errors.invalidMediaName";
    public const string InvalidMediaFile = "errors.invalidMediaFile";
    public const string MediaTooLarge = "errors.mediaTooLarge";
    public const string MediaDimensionsTooLarge = "errors.mediaDimensionsTooLarge";
    public const string MediaInUse = "errors.mediaInUse";

    public static ApiException ToApiException(NewsOutcome outcome) =>
        outcome switch
        {
            NewsOutcome.NotFound => Create(StatusCodes.Status404NotFound, ApiErrorCodes.NotFound),
            NewsOutcome.InvalidStoryId => Create(StatusCodes.Status400BadRequest, InvalidStoryId),
            NewsOutcome.InvalidTitle => Create(StatusCodes.Status400BadRequest, InvalidNewsTitle),
            NewsOutcome.InvalidBody => Create(StatusCodes.Status400BadRequest, InvalidNewsBody),
            NewsOutcome.InvalidTimes => Create(StatusCodes.Status400BadRequest, InvalidNewsTimes),
            NewsOutcome.InvalidHackingDifficulty => Create(StatusCodes.Status400BadRequest, InvalidHackingDifficulty),
            NewsOutcome.UnknownNetwork => Create(StatusCodes.Status400BadRequest, RosterErrors.UnknownNetwork),
            NewsOutcome.NetworkDisabled => Create(StatusCodes.Status400BadRequest, RosterErrors.DisabledNetworkNotSelectable),
            NewsOutcome.UnknownAuthor => Create(StatusCodes.Status400BadRequest, UnknownAuthor),
            NewsOutcome.AuthorArchived => Create(StatusCodes.Status409Conflict, AuthorArchived),
            NewsOutcome.ExternalImage => Create(StatusCodes.Status400BadRequest, ExternalImage),
            NewsOutcome.MissingMediaText => Create(StatusCodes.Status400BadRequest, MissingMediaText),
            NewsOutcome.InvalidMediaReference => Create(StatusCodes.Status400BadRequest, InvalidMediaReference),
            NewsOutcome.TooManyMedia => Create(StatusCodes.Status400BadRequest, TooManyMedia),
            NewsOutcome.UnknownMedia => Create(StatusCodes.Status400BadRequest, UnknownMedia),
            NewsOutcome.OptionsOnPicture => Create(StatusCodes.Status400BadRequest, OptionsOnPicture),
            NewsOutcome.StoryIdInUse => Create(StatusCodes.Status409Conflict, StoryIdInUse),
            NewsOutcome.ConcurrentChange => Create(StatusCodes.Status409Conflict, CatalogErrors.ConcurrentChange),
            NewsOutcome.InvalidMediaName => Create(StatusCodes.Status400BadRequest, InvalidMediaName),
            NewsOutcome.InvalidMediaFile => Create(StatusCodes.Status400BadRequest, InvalidMediaFile),
            NewsOutcome.MediaTooLarge => Create(StatusCodes.Status400BadRequest, MediaTooLarge),
            NewsOutcome.MediaDimensionsTooLarge => Create(StatusCodes.Status400BadRequest, MediaDimensionsTooLarge),
            NewsOutcome.MediaInUse => Create(StatusCodes.Status409Conflict, MediaInUse),
            _ => throw new ArgumentOutOfRangeException(nameof(outcome), outcome, "A successful outcome is not an error.")
        };

    public static T Unwrap<T>(NewsResult<T> result)
        where T : class =>
        result.Outcome == NewsOutcome.Succeeded && result.Value is { } value
            ? value
            : throw ToApiException(result.Outcome);

    private static ApiException Create(int statusCode, string code) => new(statusCode, code, code);
}
