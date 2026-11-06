namespace SciFiLarpNetwork.Backend.Infrastructure.Administration;

public sealed record AdministratorAuditDocument(
    Guid Id,
    string ActorKind,
    Guid? ActorId,
    Guid TargetAccountId,
    string EventType,
    string PreviousRole,
    string NewRole,
    DateTimeOffset OccurredAtUtc);
