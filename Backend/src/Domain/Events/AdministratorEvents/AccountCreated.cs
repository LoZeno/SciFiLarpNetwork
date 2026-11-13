namespace SciFiLarpNetwork.Backend.Domain.Events.AdministratorEvents;

public sealed record AccountCreated(
    Guid AccountId,
    string UserName,
    string NormalizedUserName,
    string PasswordHash,
    string Role,
    DateTimeOffset OccurredAtUtc);
