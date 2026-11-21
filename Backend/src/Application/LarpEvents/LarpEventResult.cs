namespace SciFiLarpNetwork.Backend.Application.LarpEvents;

/// <summary>The outcome of an event command or query; <see cref="Value"/> holds the result when it succeeded.</summary>
public sealed record LarpEventResult<T>(LarpEventOutcome Outcome, T? Value)
    where T : class;
