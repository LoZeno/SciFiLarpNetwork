namespace SciFiLarpNetwork.Backend.Application.Messages;

/// <summary>The outcome of a message command or query; <see cref="Value"/> holds the result when it succeeded.</summary>
/// <param name="CharacterIds">The characters an unknown or archived character outcome is about.</param>
public sealed record MessageResult<T>(MessageOutcome Outcome, T? Value, IReadOnlyList<Guid> CharacterIds)
    where T : class;
