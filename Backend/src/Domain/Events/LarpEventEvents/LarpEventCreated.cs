namespace SciFiLarpNetwork.Backend.Domain.Events.LarpEventEvents;

public sealed record LarpEventCreated(
    Guid EventId,
    int Number,
    string Name,
    string NormalizedName,
    string? Description,
    DateTimeOffset OccurredAtUtc);
