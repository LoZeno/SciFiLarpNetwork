namespace SciFiLarpNetwork.Management.Api.Players;

internal sealed record CharacterProperties(
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
    string Status,
    DateOnly? DeathDate,
    Guid? NetworkId,
    string QrIdentifier,
    IReadOnlyList<CharacterStatValueProperties> Stats,
    bool IsArchived,
    long Version);
