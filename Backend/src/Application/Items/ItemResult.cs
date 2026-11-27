namespace SciFiLarpNetwork.Backend.Application.Items;

/// <summary>The outcome of an item command. <see cref="Value"/> holds the resulting state when the command succeeded.</summary>
/// <param name="UnknownIds">The requested items or copies that were not found, for <see cref="ItemOutcome.UnknownItems"/>
/// and <see cref="ItemOutcome.UnknownCopies"/>.</param>
public sealed record ItemResult<T>(ItemOutcome Outcome, T? Value, IReadOnlyList<Guid> UnknownIds)
    where T : class;
