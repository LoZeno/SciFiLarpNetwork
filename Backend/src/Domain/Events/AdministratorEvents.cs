namespace SciFiLarpNetwork.Backend.Domain.Events;

public sealed record AccountCreated(
    Guid AccountId,
    string UserName,
    string NormalizedUserName,
    string PasswordHash,
    string Role,
    DateTimeOffset OccurredAtUtc);

public sealed record AccountPasswordChanged(
    Guid AccountId,
    string PasswordHash,
    DateTimeOffset OccurredAtUtc);

public sealed record AccountRoleChanged(
    Guid AccountId,
    string PreviousRole,
    string NewRole,
    Guid ActorAccountId,
    DateTimeOffset OccurredAtUtc);

public sealed record BootstrapCredentialIssued(
    string TokenHash,
    DateTimeOffset ExpiresAtUtc,
    DateTimeOffset OccurredAtUtc);

public sealed record RecoveryCredentialIssued(
    string TokenHash,
    DateTimeOffset ExpiresAtUtc,
    DateTimeOffset OccurredAtUtc);

public sealed record GrantConsumed(
    Guid TargetAccountId,
    DateTimeOffset OccurredAtUtc);

public sealed record SessionCreated(
    Guid SessionId,
    string TokenHash,
    Guid AccountId,
    DateTimeOffset ExpiresAtUtc,
    DateTimeOffset OccurredAtUtc);

public sealed record SessionRevoked(
    Guid SessionId,
    Guid AccountId,
    DateTimeOffset OccurredAtUtc);

public sealed record AuditEventLogged(
    Guid AuditId,
    string ActorKind,
    Guid? ActorAccountId,
    Guid TargetAccountId,
    string EventType,
    string PreviousRole,
    string NewRole,
    DateTimeOffset OccurredAtUtc);
