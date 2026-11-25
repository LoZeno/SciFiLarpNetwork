using SciFiLarpNetwork.Backend.Domain.News;

namespace SciFiLarpNetwork.Backend.Tests.Domain.News;

public sealed class NewsTextsTests
{
    [Theory]
    [InlineData("Fleet arrives", true)]
    [InlineData("  Fleet arrives  ", true)]
    [InlineData(null, false)]
    [InlineData("   ", false)]
    [InlineData("Fleet\narrives", false)]
    public void TitleRules(string? title, bool expected) =>
        Assert.Equal(expected, NewsTexts.IsValidTitle(title));

    [Theory]
    [InlineData(128, true)]
    [InlineData(129, false)]
    public void TitleLengthLimit(int length, bool expected) =>
        Assert.Equal(expected, NewsTexts.IsValidTitle(new string('t', length)));

    [Theory]
    [InlineData("**Fleet** arrives.\r\n\tTonight.", true)]
    [InlineData(null, false)]
    [InlineData(" \n ", false)]
    [InlineData("Fleet\0arrives", false)]
    public void BodyRules(string? body, bool expected) =>
        Assert.Equal(expected, NewsTexts.IsValidBody(body));

    [Theory]
    [InlineData(10000, true)]
    [InlineData(10001, false)]
    public void BodyLengthLimit(int length, bool expected) =>
        Assert.Equal(expected, NewsTexts.IsValidBody(new string('b', length)));

    [Theory]
    [InlineData(1, true)]
    [InlineData(0, false)]
    public void EndMustComeAfterStart(int minutesAfterStart, bool expected)
    {
        var start = new DateTimeOffset(2026, 11, 24, 9, 0, 0, TimeSpan.Zero);

        Assert.Equal(expected, NewsTexts.AreValidTimes(start, start.AddMinutes(minutesAfterStart)));
    }

    [Fact]
    public void EndBeforeStartIsInvalid()
    {
        var start = new DateTimeOffset(2026, 11, 24, 9, 0, 0, TimeSpan.Zero);

        Assert.False(NewsTexts.AreValidTimes(start, start.AddHours(-1)));
    }

    [Theory]
    [InlineData(1, true)]
    [InlineData(int.MaxValue, true)]
    [InlineData(0, false)]
    [InlineData(-3, false)]
    public void HackingDifficultyIsAtLeastOneWithNoMaximum(int difficulty, bool expected) =>
        Assert.Equal(expected, NewsTexts.IsValidHackingDifficulty(difficulty));

    [Theory]
    [InlineData("Harbour at dawn", true)]
    [InlineData(" ", false)]
    [InlineData(null, false)]
    [InlineData("Harbour\tat dawn", false)]
    public void MediaNameRules(string? name, bool expected) =>
        Assert.Equal(expected, NewsTexts.IsValidMediaName(name));

    [Theory]
    [InlineData(128, true)]
    [InlineData(129, false)]
    public void MediaNameLengthLimit(int length, bool expected) =>
        Assert.Equal(expected, NewsTexts.IsValidMediaName(new string('n', length)));
}
