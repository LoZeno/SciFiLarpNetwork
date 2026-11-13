namespace SciFiLarpNetwork.Backend.Domain.Events.AdministratorEvents;

public sealed record RecoveryCredentialIssued(
    string TokenHash,
    DateTimeOffset ExpiresAtUtc,
    DateTimeOffset OccurredAtUtc);
