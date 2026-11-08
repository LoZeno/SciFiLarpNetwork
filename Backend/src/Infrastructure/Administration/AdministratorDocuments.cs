namespace SciFiLarpNetwork.Backend.Infrastructure.Administration;

public sealed record AdministratorAccountDocument(
    Guid Id,
    string UserName,
    string NormalizedUserName,
    string PasswordHash,
    string Role,
    DateTimeOffset CreatedAtUtc);

public sealed record AdministratorSessionDocument(
    Guid Id,
    string TokenHash,
    Guid AccountId,
    DateTimeOffset ExpiresAtUtc);

public sealed record AdministratorGrantDocument(
    Guid Id,
    string Purpose,
    string TokenHash,
    DateTimeOffset ExpiresAtUtc);

public sealed record AdministratorAuditDocument(
    Guid Id,
    string ActorKind,
    Guid? ActorAccountId,
    Guid TargetAccountId,
    string EventType,
    string PreviousRole,
    string NewRole,
    DateTimeOffset OccurredAtUtc);
