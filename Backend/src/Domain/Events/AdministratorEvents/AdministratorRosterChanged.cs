namespace SciFiLarpNetwork.Backend.Domain.Events.AdministratorEvents;

/// <summary>
/// Appended to the single administrator-roster stream on every role change, so concurrent role changes conflict on
/// that stream's version and cannot together remove the last administrator.
/// </summary>
public sealed record AdministratorRosterChanged(Guid ActorAccountId, Guid TargetAccountId, DateTimeOffset OccurredAtUtc);
