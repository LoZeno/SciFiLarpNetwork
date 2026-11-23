using SciFiLarpNetwork.Backend.Api.CharacterCards;
using SciFiLarpNetwork.Backend.Application.Messages;

namespace SciFiLarpNetwork.Backend.Api.Messages;

/// <summary>Maps message outcomes to stable Siren error codes. Each code is also its localization key.</summary>
public static class MessageErrors
{
    public const string InvalidMessageId = "errors.invalidMessageId";
    public const string InvalidMessageTitle = "errors.invalidMessageTitle";
    public const string InvalidMessageBody = "errors.invalidMessageBody";
    public const string NoRecipients = "errors.noRecipients";
    public const string TooManyRecipients = "errors.tooManyRecipients";
    public const string ArchivedCharacters = "errors.archivedCharacters";
    public const string UnknownReplySource = "errors.unknownReplySource";
    public const string MessageIdInUse = "errors.messageIdInUse";

    public static ApiException ToApiException<T>(MessageResult<T> result)
        where T : class =>
        result.Outcome switch
        {
            MessageOutcome.NotFound => Create(StatusCodes.Status404NotFound, ApiErrorCodes.NotFound),
            MessageOutcome.InvalidMessageId => Create(StatusCodes.Status400BadRequest, InvalidMessageId),
            MessageOutcome.InvalidTitle => Create(StatusCodes.Status400BadRequest, InvalidMessageTitle),
            MessageOutcome.InvalidBody => Create(StatusCodes.Status400BadRequest, InvalidMessageBody),
            MessageOutcome.NoRecipients => Create(StatusCodes.Status400BadRequest, NoRecipients),
            MessageOutcome.TooManyRecipients => Create(StatusCodes.Status400BadRequest, TooManyRecipients),
            MessageOutcome.UnknownCharacters =>
                WithCharacters(StatusCodes.Status400BadRequest, CharacterCardErrors.UnknownCharacters, result.CharacterIds),
            MessageOutcome.ArchivedCharacters =>
                WithCharacters(StatusCodes.Status409Conflict, ArchivedCharacters, result.CharacterIds),
            MessageOutcome.UnknownReplySource => Create(StatusCodes.Status400BadRequest, UnknownReplySource),
            MessageOutcome.MessageIdInUse => Create(StatusCodes.Status409Conflict, MessageIdInUse),
            _ => throw new ArgumentOutOfRangeException(nameof(result), result.Outcome, "A successful outcome is not an error.")
        };

    public static T Unwrap<T>(MessageResult<T> result)
        where T : class =>
        result.Outcome == MessageOutcome.Succeeded && result.Value is { } value
            ? value
            : throw ToApiException(result);

    private static ApiException Create(int statusCode, string code) => new(statusCode, code, code);

    /// <summary>Lists the characters the error is about under the same detail key as card printing.</summary>
    private static ApiException WithCharacters(int statusCode, string code, IReadOnlyList<Guid> characterIds) =>
        new(statusCode, code, code, new Dictionary<string, IReadOnlyList<string>>
        {
            [CharacterCardErrors.CharacterIdsErrorKey] = characterIds.Select(id => id.ToString("D")).ToArray()
        });
}
