namespace SciFiLarpNetwork.Backend.Domain.Events.ItemEvents;

public sealed record ItemArchived(Guid ItemId, DateTimeOffset OccurredAtUtc);
