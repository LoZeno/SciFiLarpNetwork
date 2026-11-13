namespace SciFiLarpNetwork.Backend.Infrastructure.Administration.AdministratorDocuments;

public sealed record AdministratorGrantDocument(
    Guid Id,
    string Purpose,
    string TokenHash,
    DateTimeOffset ExpiresAtUtc);
