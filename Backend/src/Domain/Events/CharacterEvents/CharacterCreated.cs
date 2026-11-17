using SciFiLarpNetwork.Backend.Domain.Characters;

namespace SciFiLarpNetwork.Backend.Domain.Events.CharacterEvents;

/// <summary>A new character is Active.</summary>
public sealed record CharacterCreated(
    Guid CharacterId,
    int Number,
    Guid PlayerId,
    string Name,
    string NormalizedName,
    string? Title,
    string? Species,
    string? Gender,
    string? Type,
    int ExperiencePoints,
    Guid? NetworkId,
    string QrIdentifier,
    IReadOnlyList<CharacterStatValue> StatValues,
    DateTimeOffset OccurredAtUtc);
