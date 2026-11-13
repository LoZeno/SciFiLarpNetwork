namespace SciFiLarpNetwork.Backend.Domain.Events.AdministratorEvents;

public sealed record AccountRoleChanged(
    Guid AccountId,
    string PreviousRole,
    string NewRole,
    Guid ActorAccountId,
    DateTimeOffset OccurredAtUtc);
