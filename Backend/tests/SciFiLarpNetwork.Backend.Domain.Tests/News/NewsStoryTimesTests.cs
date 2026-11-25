using SciFiLarpNetwork.Backend.Domain.News;

namespace SciFiLarpNetwork.Backend.Tests.Domain.News;

public sealed class NewsStoryTimesTests
{
    private static readonly DateTimeOffset Start = new(2026, 11, 24, 9, 0, 0, TimeSpan.Zero);
    private static readonly DateTimeOffset End = Start.AddHours(2);

    [Theory]
    [InlineData(-1, false)]
    [InlineData(0, true)]
    [InlineData(119, true)]
    [InlineData(120, false)]
    public void StoryIsActiveFromItsStartUpToButNotIncludingItsEnd(int minutesAfterStart, bool expected) =>
        Assert.Equal(expected, NewsStoryTimes.IsActiveAt(Start, End, Start.AddMinutes(minutesAfterStart)));

    [Theory]
    [InlineData(-1, NewsStoryStatus.Scheduled)]
    [InlineData(0, NewsStoryStatus.Active)]
    [InlineData(119, NewsStoryStatus.Active)]
    [InlineData(120, NewsStoryStatus.Expired)]
    public void StatusComparesTheTimesWithNow(int minutesAfterStart, NewsStoryStatus expected) =>
        Assert.Equal(expected, NewsStoryTimes.StatusAt(Start, End, Start.AddMinutes(minutesAfterStart)));

    [Fact]
    public void StoredPrecisionIsUtcCutToWholeMicroseconds() =>
        Assert.Equal(
            new DateTimeOffset(2026, 11, 24, 8, 0, 0, TimeSpan.Zero).AddTicks(1234560),
            NewsStoryTimes.ToStoredPrecision(new DateTimeOffset(2026, 11, 24, 9, 0, 0, TimeSpan.FromHours(1)).AddTicks(1234567)));

    [Fact]
    public void StoredPrecisionIsInUtc() =>
        Assert.Equal(TimeSpan.Zero, NewsStoryTimes.ToStoredPrecision(new DateTimeOffset(2026, 11, 24, 9, 0, 0, TimeSpan.FromHours(1))).Offset);

    [Fact]
    public void TimesInOtherOffsetsCompareAsInstants() =>
        Assert.True(NewsStoryTimes.IsActiveAt(Start, End, new DateTimeOffset(2026, 11, 24, 10, 30, 0, TimeSpan.FromHours(1))));
}
