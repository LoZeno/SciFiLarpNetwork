namespace SciFiLarpNetwork.Management.Api.Players;

internal sealed record CharacterStatValueProperties(
    Guid StatId,
    string Name,
    string Type,
    int? Number,
    string? Text,
    bool IsArchived);
