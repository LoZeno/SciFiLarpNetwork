namespace SciFiLarpNetwork.Backend.Infrastructure.Administration.AdministratorDocuments;

public sealed record AdministratorSessionDocument(
    Guid Id,
    string TokenHash,
    Guid AccountId,
    DateTimeOffset ExpiresAtUtc);
