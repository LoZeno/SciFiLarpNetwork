namespace SciFiLarpNetwork.Backend.Application.Administration;

public sealed record AdministratorSummary(
    Guid AccountId,
    string UserName,
    string Role,
    DateTimeOffset CreatedAtUtc);
