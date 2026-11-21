using SciFiLarpNetwork.Backend.Api.LarpEvents;
using SciFiLarpNetwork.Backend.Application.LarpEvents;

namespace SciFiLarpNetwork.Backend.Tests.Host;

public sealed class LarpEventErrorsTests
{
    [Theory]
    [InlineData(LarpEventOutcome.NotFound, 404, "errors.notFound")]
    [InlineData(LarpEventOutcome.StaleVersion, 409, "errors.staleVersion")]
    [InlineData(LarpEventOutcome.ConcurrentChange, 409, "errors.concurrentChange")]
    [InlineData(LarpEventOutcome.InvalidName, 400, "errors.invalidName")]
    [InlineData(LarpEventOutcome.InvalidDescription, 400, "errors.invalidDescription")]
    [InlineData(LarpEventOutcome.InvalidExperienceValue, 400, "errors.invalidExperienceValue")]
    [InlineData(LarpEventOutcome.AwardNotAllowed, 409, "errors.awardNotAllowed")]
    [InlineData(LarpEventOutcome.EventClosed, 409, "errors.eventClosed")]
    [InlineData(LarpEventOutcome.EventNotClosed, 409, "errors.eventNotClosed")]
    [InlineData(LarpEventOutcome.Archived, 409, "errors.archived")]
    [InlineData(LarpEventOutcome.DuplicateDate, 409, "errors.duplicateDate")]
    [InlineData(LarpEventOutcome.UnknownDay, 400, "errors.unknownDay")]
    [InlineData(LarpEventOutcome.DayHasParticipants, 409, "errors.dayHasParticipants")]
    [InlineData(LarpEventOutcome.NoDaysSelected, 400, "errors.noDaysSelected")]
    [InlineData(LarpEventOutcome.UnknownCharacter, 400, "errors.unknownCharacter")]
    [InlineData(LarpEventOutcome.CharacterNotActive, 409, "errors.characterNotActive")]
    [InlineData(LarpEventOutcome.AlreadyRegistered, 409, "errors.alreadyRegistered")]
    [InlineData(LarpEventOutcome.NotRegistered, 409, "errors.notRegistered")]
    [InlineData(LarpEventOutcome.InvalidExperienceChange, 400, "errors.invalidExperienceChange")]
    [InlineData(LarpEventOutcome.ExperienceOutOfRange, 409, "errors.experienceOutOfRange")]
    [InlineData(LarpEventOutcome.InsufficientFreePoints, 409, "errors.insufficientFreePoints")]
    [InlineData(LarpEventOutcome.NoParticipants, 400, "errors.noParticipants")]
    [InlineData(LarpEventOutcome.TooManyCards, 400, "errors.tooManyCharacters")]
    public void OutcomeHasItsStableCode(LarpEventOutcome outcome, int statusCode, string code)
    {
        var exception = LarpEventErrors.ToApiException(outcome);

        Assert.Equal(new { StatusCode = statusCode, Code = code }, new { exception.StatusCode, exception.Code });
    }

    [Fact]
    public void SuccessIsNotAnError() =>
        Assert.Throws<ArgumentOutOfRangeException>(() => LarpEventErrors.ToApiException(LarpEventOutcome.Succeeded));
}
