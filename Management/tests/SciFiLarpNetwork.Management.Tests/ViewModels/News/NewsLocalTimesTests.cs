using SciFiLarpNetwork.Management.ViewModels.News;

namespace SciFiLarpNetwork.Management.Tests.ViewModels.News;

public sealed class NewsLocalTimesTests
{
    /// <summary>A zone one hour ahead of UTC that moves its clocks forward at 02:00 on 29 March 2026.</summary>
    private static readonly TimeZoneInfo Changing = TimeZoneInfo.CreateCustomTimeZone(
        "Test changing",
        TimeSpan.FromHours(1),
        "Test changing",
        "Test standard",
        "Test summer",
        [
            TimeZoneInfo.AdjustmentRule.CreateAdjustmentRule(
                new DateTime(2026, 1, 1),
                new DateTime(2026, 12, 31),
                TimeSpan.FromHours(1),
                TimeZoneInfo.TransitionTime.CreateFixedDateRule(new DateTime(1, 1, 1, 2, 0, 0), 3, 29),
                TimeZoneInfo.TransitionTime.CreateFixedDateRule(new DateTime(1, 1, 1, 3, 0, 0), 10, 25))
        ]);

    [Fact]
    public void LocalDateAndTimeBecomeAnInstantInUtc() =>
        Assert.Equal(
            new DateTimeOffset(2026, 11, 24, 12, 30, 0, TimeSpan.Zero),
            NewsLocalTimes.ToInstant(new DateTime(2026, 11, 24), new TimeSpan(13, 30, 0), NewsScreens.PlusOne));

    [Fact]
    public void SecondsAreDropped() =>
        Assert.Equal(
            new DateTimeOffset(2026, 11, 24, 13, 30, 0, TimeSpan.Zero),
            NewsLocalTimes.ToInstant(new DateTime(2026, 11, 24), new TimeSpan(13, 30, 45), TimeZoneInfo.Utc));

    [Fact]
    public void MissingTimeGivesNoInstant() =>
        Assert.Null(NewsLocalTimes.ToInstant(new DateTime(2026, 11, 24), null, TimeZoneInfo.Utc));

    [Fact]
    public void TimeSkippedWhenClocksGoForwardGivesNoInstant() =>
        Assert.Null(NewsLocalTimes.ToInstant(new DateTime(2026, 3, 29), new TimeSpan(2, 30, 0), Changing));

    [Fact]
    public void SummerTimeUsesItsOwnOffset() =>
        Assert.Equal(
            new DateTimeOffset(2026, 7, 1, 10, 0, 0, TimeSpan.Zero),
            NewsLocalTimes.ToInstant(new DateTime(2026, 7, 1), new TimeSpan(12, 0, 0), Changing));

    [Fact]
    public void InstantIsShownInTheZoneToTheMinute() =>
        Assert.Equal(
            new DateTimeOffset(2026, 11, 24, 13, 30, 0, TimeSpan.FromHours(1)),
            NewsLocalTimes.ToLocal(new DateTimeOffset(2026, 11, 24, 12, 30, 59, TimeSpan.Zero), NewsScreens.PlusOne));
}
