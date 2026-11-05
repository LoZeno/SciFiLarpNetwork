namespace SciFiLarpNetwork.Backend.Application.Administration;

public sealed record AdministratorSession(
    Guid AccountId,
    string UserName,
    string Role,
    DateTimeOffset ExpiresAtUtc,
    string SessionToken);