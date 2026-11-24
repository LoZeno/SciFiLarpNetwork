namespace SciFiLarpNetwork.Backend.Domain.Events.NewsEvents;

/// <summary>The media left the library and its file was deleted; no story used it.</summary>
public sealed record NewsMediaRemoved(Guid MediaId, DateTimeOffset OccurredAtUtc);
