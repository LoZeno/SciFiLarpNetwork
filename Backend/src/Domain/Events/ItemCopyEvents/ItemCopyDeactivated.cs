namespace SciFiLarpNetwork.Backend.Domain.Events.ItemCopyEvents;

public sealed record ItemCopyDeactivated(Guid CopyId, Guid ItemId, DateTimeOffset OccurredAtUtc);
