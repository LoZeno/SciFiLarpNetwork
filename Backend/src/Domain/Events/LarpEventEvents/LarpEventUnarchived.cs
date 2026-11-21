namespace SciFiLarpNetwork.Backend.Domain.Events.LarpEventEvents;

public sealed record LarpEventUnarchived(Guid EventId, DateTimeOffset OccurredAtUtc);
