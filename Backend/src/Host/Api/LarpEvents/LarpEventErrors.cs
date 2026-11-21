using SciFiLarpNetwork.Backend.Api.CharacterCards;
using SciFiLarpNetwork.Backend.Api.Players;
using SciFiLarpNetwork.Backend.Api.Skills;
using SciFiLarpNetwork.Backend.Application.LarpEvents;

namespace SciFiLarpNetwork.Backend.Api.LarpEvents;

/// <summary>Maps event command outcomes to stable Siren error codes. Each code is also its localization key.</summary>
public static class LarpEventErrors
{
    public const string InvalidDescription = "errors.invalidDescription";
    public const string EventClosed = "errors.eventClosed";
    public const string EventNotClosed = "errors.eventNotClosed";
    public const string DuplicateDate = "errors.duplicateDate";
    public const string UnknownDay = "errors.unknownDay";
    public const string DayHasParticipants = "errors.dayHasParticipants";
    public const string NoDaysSelected = "errors.noDaysSelected";
    public const string UnknownCharacter = "errors.unknownCharacter";
    public const string CharacterNotActive = "errors.characterNotActive";
    public const string AlreadyRegistered = "errors.alreadyRegistered";
    public const string NotRegistered = "errors.notRegistered";
    public const string InvalidExperienceChange = "errors.invalidExperienceChange";
    public const string ExperienceOutOfRange = "errors.experienceOutOfRange";
    public const string NoParticipants = "errors.noParticipants";

    public static ApiException ToApiException(LarpEventOutcome outcome) =>
        outcome switch
        {
            LarpEventOutcome.NotFound => Create(StatusCodes.Status404NotFound, ApiErrorCodes.NotFound),
            LarpEventOutcome.StaleVersion => Create(StatusCodes.Status409Conflict, CatalogErrors.StaleVersion),
            LarpEventOutcome.ConcurrentChange => Create(StatusCodes.Status409Conflict, CatalogErrors.ConcurrentChange),
            LarpEventOutcome.InvalidName => Create(StatusCodes.Status400BadRequest, CatalogErrors.InvalidName),
            LarpEventOutcome.InvalidDescription => Create(StatusCodes.Status400BadRequest, InvalidDescription),
            LarpEventOutcome.EventClosed => Create(StatusCodes.Status409Conflict, EventClosed),
            LarpEventOutcome.EventNotClosed => Create(StatusCodes.Status409Conflict, EventNotClosed),
            LarpEventOutcome.Archived => Create(StatusCodes.Status409Conflict, RosterErrors.Archived),
            LarpEventOutcome.DuplicateDate => Create(StatusCodes.Status409Conflict, DuplicateDate),
            LarpEventOutcome.UnknownDay => Create(StatusCodes.Status400BadRequest, UnknownDay),
            LarpEventOutcome.DayHasParticipants => Create(StatusCodes.Status409Conflict, DayHasParticipants),
            LarpEventOutcome.NoDaysSelected => Create(StatusCodes.Status400BadRequest, NoDaysSelected),
            LarpEventOutcome.UnknownCharacter => Create(StatusCodes.Status400BadRequest, UnknownCharacter),
            LarpEventOutcome.CharacterNotActive => Create(StatusCodes.Status409Conflict, CharacterNotActive),
            LarpEventOutcome.AlreadyRegistered => Create(StatusCodes.Status409Conflict, AlreadyRegistered),
            LarpEventOutcome.NotRegistered => Create(StatusCodes.Status409Conflict, NotRegistered),
            LarpEventOutcome.InvalidExperienceChange => Create(StatusCodes.Status400BadRequest, InvalidExperienceChange),
            LarpEventOutcome.ExperienceOutOfRange => Create(StatusCodes.Status409Conflict, ExperienceOutOfRange),
            LarpEventOutcome.InsufficientFreePoints => Create(StatusCodes.Status409Conflict, RosterErrors.InsufficientFreePoints),
            LarpEventOutcome.NoParticipants => Create(StatusCodes.Status400BadRequest, NoParticipants),
            LarpEventOutcome.TooManyCards => Create(StatusCodes.Status400BadRequest, CharacterCardErrors.TooManyCharacters),
            _ => throw new ArgumentOutOfRangeException(nameof(outcome), outcome, "A successful outcome is not an error.")
        };

    public static T Unwrap<T>(LarpEventResult<T> result)
        where T : class =>
        result.Outcome == LarpEventOutcome.Succeeded && result.Value is { } value
            ? value
            : throw ToApiException(result.Outcome);

    private static ApiException Create(int statusCode, string code) => new(statusCode, code, code);
}
