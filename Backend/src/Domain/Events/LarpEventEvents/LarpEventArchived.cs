namespace SciFiLarpNetwork.Backend.Domain.Events.LarpEventEvents;

public sealed record LarpEventArchived(Guid EventId, DateTimeOffset OccurredAtUtc);
