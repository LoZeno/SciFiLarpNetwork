namespace SciFiLarpNetwork.Backend.Infrastructure.Administration;

public sealed record AdministratorGrantDocument(
    Guid Id,
    string Purpose,
    string TokenHash,
    DateTimeOffset ExpiresAtUtc);
