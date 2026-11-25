namespace SciFiLarpNetwork.Backend.Domain.News;

public static class NewsStoryTimes
{
    /// <summary>Whether a story is active at <paramref name="time"/>: from its start up to, but not including, its end.</summary>
    public static bool IsActiveAt(DateTimeOffset startsAt, DateTimeOffset endsAt, DateTimeOffset time) =>
        startsAt <= time && time < endsAt;

    /// <summary>
    /// The instant in UTC, cut to whole microseconds. PostgreSQL keeps times to the microsecond, so times stored and
    /// compared at this precision compare exactly; finer .NET ticks could round across a story's start or end.
    /// </summary>
    public static DateTimeOffset ToStoredPrecision(DateTimeOffset time)
    {
        var utc = time.ToUniversalTime();
        return utc.AddTicks(-(utc.Ticks % TimeSpan.TicksPerMicrosecond));
    }

    public static NewsStoryStatus StatusAt(DateTimeOffset startsAt, DateTimeOffset endsAt, DateTimeOffset now) =>
        now < startsAt ? NewsStoryStatus.Scheduled
        : now < endsAt ? NewsStoryStatus.Active
        : NewsStoryStatus.Expired;
}
