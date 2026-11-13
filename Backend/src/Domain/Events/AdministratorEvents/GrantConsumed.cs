namespace SciFiLarpNetwork.Backend.Domain.Events.AdministratorEvents;

public sealed record GrantConsumed(
    Guid TargetAccountId,
    DateTimeOffset OccurredAtUtc);
