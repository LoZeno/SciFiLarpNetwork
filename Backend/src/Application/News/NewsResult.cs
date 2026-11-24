namespace SciFiLarpNetwork.Backend.Application.News;

/// <summary>The outcome of a news command; <see cref="Value"/> holds the result when it succeeded.</summary>
public sealed record NewsResult<T>(NewsOutcome Outcome, T? Value)
    where T : class;
