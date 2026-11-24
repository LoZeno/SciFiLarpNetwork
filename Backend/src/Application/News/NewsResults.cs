namespace SciFiLarpNetwork.Backend.Application.News;

public static class NewsResults
{
    public static NewsResult<T> Success<T>(T value)
        where T : class => new(NewsOutcome.Succeeded, value);

    public static NewsResult<T> Failure<T>(NewsOutcome outcome)
        where T : class => new(outcome, null);
}
