namespace SciFiLarpNetwork.Backend.Application.Skills;

/// <summary>
/// The outcome of a catalog command. <see cref="Value"/> holds the resulting state when the command succeeded.
/// </summary>
public sealed record CatalogResult<T>(CatalogOutcome Outcome, T? Value)
    where T : class;
