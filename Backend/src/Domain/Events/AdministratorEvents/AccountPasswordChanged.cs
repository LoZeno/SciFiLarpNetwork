namespace SciFiLarpNetwork.Backend.Domain.Events.AdministratorEvents;

public sealed record AccountPasswordChanged(
    Guid AccountId,
    string PasswordHash,
    DateTimeOffset OccurredAtUtc);
