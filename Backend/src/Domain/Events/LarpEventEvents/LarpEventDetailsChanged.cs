namespace SciFiLarpNetwork.Backend.Domain.Events.LarpEventEvents;

public sealed record LarpEventDetailsChanged(
    Guid EventId,
    string Name,
    string NormalizedName,
    string? Description,
    DateTimeOffset OccurredAtUtc);
