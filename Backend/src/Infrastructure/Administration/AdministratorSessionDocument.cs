namespace SciFiLarpNetwork.Backend.Infrastructure.Administration;

public sealed record AdministratorSessionDocument(
    Guid Id,
    string TokenHash,
    Guid AccountId,
    DateTimeOffset ExpiresAtUtc);
