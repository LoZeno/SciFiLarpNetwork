namespace SciFiLarpNetwork.Backend.Infrastructure.Administration.AdministratorDocuments;

public sealed record AdministratorAuditDocument(
    Guid Id,
    string ActorKind,
    Guid? ActorAccountId,
    Guid TargetAccountId,
    string EventType,
    string PreviousRole,
    string NewRole,
    DateTimeOffset OccurredAtUtc);
