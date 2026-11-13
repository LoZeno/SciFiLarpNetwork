namespace SciFiLarpNetwork.Backend.Domain.Events.AdministratorEvents;

public sealed record SessionCreated(
    Guid SessionId,
    string TokenHash,
    Guid AccountId,
    DateTimeOffset ExpiresAtUtc,
    DateTimeOffset OccurredAtUtc);
