namespace SciFiLarpNetwork.Backend.Domain.Events.RunningNumberEvents;

/// <summary>
/// Appended to a running-number sequence stream (one for players, one for characters) for every number handed
/// out, so two concurrent creations conflict on the sequence version instead of taking the same number.
/// </summary>
public sealed record RunningNumberIssued(int Number, Guid IssuedTo, DateTimeOffset OccurredAtUtc);
