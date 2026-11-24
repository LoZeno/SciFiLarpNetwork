using SciFiLarpNetwork.Backend.Application.News;

namespace SciFiLarpNetwork.Backend.Tests.Application.News;

public sealed class NewsStoryContentValidatorTests
{
    private static readonly DateTimeOffset Start = new(2026, 11, 24, 9, 0, 0, TimeSpan.Zero);

    private static NewsStoryContent Valid => new(
        Guid.Parse("00000000-0000-0000-0000-000000000001"),
        Guid.Parse("00000000-0000-0000-0000-000000000002"),
        "Fleet arrives",
        "The fleet arrives **tonight**.",
        Start,
        Start.AddHours(4),
        1);

    public static TheoryData<NewsStoryContent, NewsOutcome> InvalidContent => new()
    {
        { Valid with { Title = null }, NewsOutcome.InvalidTitle },
        { Valid with { Title = new string('t', 129) }, NewsOutcome.InvalidTitle },
        { Valid with { Body = " " }, NewsOutcome.InvalidBody },
        { Valid with { Body = new string('b', 10001) }, NewsOutcome.InvalidBody },
        { Valid with { Body = "Fleet\0" }, NewsOutcome.InvalidBody },
        { Valid with { EndsAt = Start }, NewsOutcome.InvalidTimes },
        { Valid with { EndsAt = Start.AddHours(-1) }, NewsOutcome.InvalidTimes },
        { Valid with { HackingDifficulty = 0 }, NewsOutcome.InvalidHackingDifficulty },
        { Valid with { Title = " ", HackingDifficulty = 0 }, NewsOutcome.InvalidTitle }
    };

    [Theory]
    [MemberData(nameof(InvalidContent))]
    public void InvalidContentIsRejectedWithItsFirstFailedCheck(NewsStoryContent content, NewsOutcome expected) =>
        Assert.Equal(expected, NewsStoryContentValidator.Validate(content));

    [Fact]
    public void CompleteContentIsValid() =>
        Assert.Null(NewsStoryContentValidator.Validate(Valid));

    [Fact]
    public void HighDifficultyIsValid() =>
        Assert.Null(NewsStoryContentValidator.Validate(Valid with { HackingDifficulty = 1000 }));

    [Fact]
    public void TimesInDifferentOffsetsCompareAsInstants() =>
        Assert.Null(NewsStoryContentValidator.Validate(Valid with
        {
            StartsAt = new DateTimeOffset(2026, 11, 24, 10, 0, 0, TimeSpan.FromHours(1)),
            EndsAt = new DateTimeOffset(2026, 11, 24, 9, 30, 0, TimeSpan.Zero)
        }));
}
