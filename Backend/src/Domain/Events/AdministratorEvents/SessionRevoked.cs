namespace SciFiLarpNetwork.Backend.Domain.Events.AdministratorEvents;

public sealed record SessionRevoked(
    Guid SessionId,
    Guid AccountId,
    DateTimeOffset OccurredAtUtc);
