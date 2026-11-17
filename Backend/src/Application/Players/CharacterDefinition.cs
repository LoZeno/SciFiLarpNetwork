using SciFiLarpNetwork.Backend.Domain.Characters;

namespace SciFiLarpNetwork.Backend.Application.Players;

/// <param name="DeathDate">Required when <paramref name="Status"/> is Dead; ignored otherwise.</param>
/// <param name="NetworkId">The optional non-default network. The default network is never stored on a character.</param>
/// <param name="StatValues">Values to set; the character's values for stats not listed are unchanged.</param>
public sealed record CharacterDefinition(
    string Name,
    string? Title,
    string? Species,
    string? Gender,
    string? Type,
    int ExperiencePoints,
    CharacterStatus Status,
    DateOnly? DeathDate,
    Guid? NetworkId,
    IReadOnlyList<CharacterStatInput> StatValues,
    IReadOnlyList<NewCharacterStat> NewStats);
