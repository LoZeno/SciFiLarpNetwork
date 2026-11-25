using SciFiLarpNetwork.Backend.Api.News;
using SciFiLarpNetwork.Backend.Application.News;

namespace SciFiLarpNetwork.Backend.Tests.Host;

public sealed class NewsErrorsTests
{
    [Theory]
    [InlineData(NewsOutcome.NotFound, 404, "errors.notFound")]
    [InlineData(NewsOutcome.InvalidStoryId, 400, "errors.invalidStoryId")]
    [InlineData(NewsOutcome.InvalidTitle, 400, "errors.invalidNewsTitle")]
    [InlineData(NewsOutcome.InvalidBody, 400, "errors.invalidNewsBody")]
    [InlineData(NewsOutcome.InvalidTimes, 400, "errors.invalidNewsTimes")]
    [InlineData(NewsOutcome.InvalidHackingDifficulty, 400, "errors.invalidHackingDifficulty")]
    [InlineData(NewsOutcome.UnknownNetwork, 400, "errors.unknownNetwork")]
    [InlineData(NewsOutcome.NetworkDisabled, 400, "errors.disabledNetworkNotSelectable")]
    [InlineData(NewsOutcome.UnknownAuthor, 400, "errors.unknownAuthor")]
    [InlineData(NewsOutcome.AuthorArchived, 409, "errors.authorArchived")]
    [InlineData(NewsOutcome.ExternalImage, 400, "errors.externalImage")]
    [InlineData(NewsOutcome.MissingMediaText, 400, "errors.missingMediaText")]
    [InlineData(NewsOutcome.InvalidMediaReference, 400, "errors.invalidMediaReference")]
    [InlineData(NewsOutcome.TooManyMedia, 400, "errors.tooManyMedia")]
    [InlineData(NewsOutcome.UnknownMedia, 400, "errors.unknownMedia")]
    [InlineData(NewsOutcome.OptionsOnPicture, 400, "errors.optionsOnPicture")]
    [InlineData(NewsOutcome.StoryIdInUse, 409, "errors.storyIdInUse")]
    [InlineData(NewsOutcome.ConcurrentChange, 409, "errors.concurrentChange")]
    [InlineData(NewsOutcome.InvalidMediaName, 400, "errors.invalidMediaName")]
    [InlineData(NewsOutcome.InvalidMediaFile, 400, "errors.invalidMediaFile")]
    [InlineData(NewsOutcome.MediaTooLarge, 400, "errors.mediaTooLarge")]
    [InlineData(NewsOutcome.MediaDimensionsTooLarge, 400, "errors.mediaDimensionsTooLarge")]
    [InlineData(NewsOutcome.MediaInUse, 409, "errors.mediaInUse")]
    public void OutcomeHasItsStableCode(NewsOutcome outcome, int statusCode, string code)
    {
        var exception = NewsErrors.ToApiException(outcome);

        Assert.Equal(new { StatusCode = statusCode, Code = code }, new { exception.StatusCode, exception.Code });
    }

    [Fact]
    public void SuccessIsNotAnError() =>
        Assert.Throws<ArgumentOutOfRangeException>(() => NewsErrors.ToApiException(NewsOutcome.Succeeded));

    [Fact]
    public void UnwrapReturnsTheValueOfASuccess() =>
        Assert.Equal("saved", NewsErrors.Unwrap(new NewsResult<string>(NewsOutcome.Succeeded, "saved")));

    [Fact]
    public void UnwrapThrowsTheErrorOfAFailure() =>
        Assert.Equal("errors.mediaInUse",
            Assert.Throws<SciFiLarpNetwork.Backend.Api.ApiException>(() =>
                NewsErrors.Unwrap(new NewsResult<string>(NewsOutcome.MediaInUse, null))).Code);
}
