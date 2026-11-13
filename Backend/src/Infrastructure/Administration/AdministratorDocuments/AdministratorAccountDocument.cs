namespace SciFiLarpNetwork.Backend.Infrastructure.Administration.AdministratorDocuments;

public sealed record AdministratorAccountDocument(
    Guid Id,
    string UserName,
    string NormalizedUserName,
    string PasswordHash,
    string Role,
    DateTimeOffset CreatedAtUtc);
