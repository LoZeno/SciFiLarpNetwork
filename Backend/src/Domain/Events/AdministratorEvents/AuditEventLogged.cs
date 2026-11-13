namespace SciFiLarpNetwork.Backend.Domain.Events.AdministratorEvents;

public sealed record AuditEventLogged(
    Guid AuditId,
    string ActorKind,
    Guid? ActorAccountId,
    Guid TargetAccountId,
    string EventType,
    string PreviousRole,
    string NewRole,
    DateTimeOffset OccurredAtUtc);
