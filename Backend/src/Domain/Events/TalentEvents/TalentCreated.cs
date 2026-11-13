namespace SciFiLarpNetwork.Backend.Domain.Events.TalentEvents;

public sealed record TalentCreated(
    Guid TalentId,
    string Name,
    string NormalizedName,
    string? Description,
    DateTimeOffset OccurredAtUtc);
