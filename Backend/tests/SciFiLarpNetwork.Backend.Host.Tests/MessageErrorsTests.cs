using SciFiLarpNetwork.Backend.Api.Messages;
using SciFiLarpNetwork.Backend.Application.Messages;

namespace SciFiLarpNetwork.Backend.Tests.Host;

public sealed class MessageErrorsTests
{
    private static readonly Guid CharacterId = Guid.Parse("00000000-0000-0000-0000-000000000007");

    [Theory]
    [InlineData(MessageOutcome.NotFound, 404, "errors.notFound")]
    [InlineData(MessageOutcome.InvalidMessageId, 400, "errors.invalidMessageId")]
    [InlineData(MessageOutcome.InvalidTitle, 400, "errors.invalidMessageTitle")]
    [InlineData(MessageOutcome.InvalidBody, 400, "errors.invalidMessageBody")]
    [InlineData(MessageOutcome.NoRecipients, 400, "errors.noRecipients")]
    [InlineData(MessageOutcome.TooManyRecipients, 400, "errors.tooManyRecipients")]
    [InlineData(MessageOutcome.UnknownCharacters, 400, "errors.unknownCharacters")]
    [InlineData(MessageOutcome.ArchivedCharacters, 409, "errors.archivedCharacters")]
    [InlineData(MessageOutcome.UnknownReplySource, 400, "errors.unknownReplySource")]
    [InlineData(MessageOutcome.MessageIdInUse, 409, "errors.messageIdInUse")]
    public void OutcomeHasItsStableCode(MessageOutcome outcome, int statusCode, string code)
    {
        var exception = MessageErrors.ToApiException(new MessageResult<string>(outcome, null, []));

        Assert.Equal(new { StatusCode = statusCode, Code = code }, new { exception.StatusCode, exception.Code });
    }

    [Theory]
    [InlineData(MessageOutcome.UnknownCharacters)]
    [InlineData(MessageOutcome.ArchivedCharacters)]
    public void CharacterOutcomeListsItsCharacters(MessageOutcome outcome)
    {
        var exception = MessageErrors.ToApiException(new MessageResult<string>(outcome, null, [CharacterId]));

        Assert.Equal(["00000000-0000-0000-0000-000000000007"], exception.Errors!["characterIds"]);
    }

    [Fact]
    public void SuccessIsNotAnError() =>
        Assert.Throws<ArgumentOutOfRangeException>(() =>
            MessageErrors.ToApiException(new MessageResult<string>(MessageOutcome.Succeeded, "sent", [])));

    [Fact]
    public void UnwrapReturnsTheValueOfASuccess() =>
        Assert.Equal("sent", MessageErrors.Unwrap(new MessageResult<string>(MessageOutcome.Succeeded, "sent", [])));
}
