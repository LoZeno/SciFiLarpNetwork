using SciFiLarpNetwork.Backend.Domain.Characters;

namespace SciFiLarpNetwork.Backend.Application.Players;

/// <param name="QrIdentifier">The public, printed QR identifier. It is never an authentication credential.</param>
/// <param name="Stats">Every stat, in name order, with this character's value.</param>
public sealed record CharacterSummary(
    Guid CharacterId,
    int Number,
    Guid PlayerId,
    string PlayerDisplayName,
    string Name,
    string? Title,
    string? Species,
    string? Gender,
    string? Type,
    int ExperiencePoints,
    CharacterStatus Status,
    DateOnly? DeathDate,
    Guid? NetworkId,
    string QrIdentifier,
    IReadOnlyList<CharacterStatValueSummary> Stats,
    bool IsArchived,
    long Version);
