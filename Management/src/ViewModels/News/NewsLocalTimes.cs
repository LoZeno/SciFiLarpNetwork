namespace SciFiLarpNetwork.Management.ViewModels.News;

/// <summary>Turns the date and time typed in the computer's time zone into an exact instant, and back.</summary>
public static class NewsLocalTimes
{
    /// <summary>
    /// The instant of a local date and time, or <see langword="null"/> when either is missing or the time does not
    /// exist in <paramref name="timeZone"/>, as in the hour skipped when clocks go forward.
    /// </summary>
    public static DateTimeOffset? ToInstant(DateTime? date, TimeSpan? time, TimeZoneInfo timeZone)
    {
        if (date is not { } day || time is not { } clock)
        {
            return null;
        }

        var local = DateTime.SpecifyKind(day.Date + new TimeSpan(clock.Hours, clock.Minutes, 0), DateTimeKind.Unspecified);
        return timeZone.IsInvalidTime(local)
            ? null
            : new DateTimeOffset(TimeZoneInfo.ConvertTimeToUtc(local, timeZone), TimeSpan.Zero);
    }

    /// <summary>The instant in <paramref name="timeZone"/>, to the minute.</summary>
    public static DateTimeOffset ToLocal(DateTimeOffset instant, TimeZoneInfo timeZone)
    {
        var local = TimeZoneInfo.ConvertTime(instant, timeZone);
        return new DateTimeOffset(local.Year, local.Month, local.Day, local.Hour, local.Minute, 0, local.Offset);
    }
}
