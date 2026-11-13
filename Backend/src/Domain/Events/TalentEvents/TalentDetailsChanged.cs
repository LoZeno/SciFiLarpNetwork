namespace SciFiLarpNetwork.Backend.Domain.Events.TalentEvents;

public sealed record TalentDetailsChanged(
    Guid TalentId,
    string Name,
    string NormalizedName,
    string? Description,
    DateTimeOffset OccurredAtUtc);
