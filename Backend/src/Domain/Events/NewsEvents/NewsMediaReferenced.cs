namespace SciFiLarpNetwork.Backend.Domain.Events.NewsEvents;

/// <summary>
/// A story started using the media. It is appended to the media's stream, so a removal decided on an older version
/// of the media conflicts with it instead of removing media a story now shows.
/// </summary>
public sealed record NewsMediaReferenced(Guid MediaId, Guid StoryId, DateTimeOffset OccurredAtUtc);
