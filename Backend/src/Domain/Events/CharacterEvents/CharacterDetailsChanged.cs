namespace SciFiLarpNetwork.Backend.Domain.Events.CharacterEvents;

public sealed record CharacterDetailsChanged(
    Guid CharacterId,
    string Name,
    string NormalizedName,
    string? Title,
    string? Species,
    string? Gender,
    string? Type,
    int ExperiencePoints,
    DateTimeOffset OccurredAtUtc);
