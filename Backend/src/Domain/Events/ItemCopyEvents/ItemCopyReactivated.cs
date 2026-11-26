namespace SciFiLarpNetwork.Backend.Domain.Events.ItemCopyEvents;

public sealed record ItemCopyReactivated(Guid CopyId, Guid ItemId, DateTimeOffset OccurredAtUtc);
