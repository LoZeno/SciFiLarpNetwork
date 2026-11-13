namespace SciFiLarpNetwork.Backend.Domain.Events.AdministratorEvents;

public sealed record BootstrapCredentialIssued(
    string TokenHash,
    DateTimeOffset ExpiresAtUtc,
    DateTimeOffset OccurredAtUtc);
