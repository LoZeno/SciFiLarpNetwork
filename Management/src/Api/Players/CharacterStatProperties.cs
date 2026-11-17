namespace SciFiLarpNetwork.Management.Api.Players;

/// <param name="DefaultNumber">The value of a Number stat for characters without their own value.</param>
/// <param name="DefaultText">The value of a Text stat for characters without their own value.</param>
internal sealed record CharacterStatProperties(
    Guid StatId,
    string Name,
    string Type,
    int? DefaultNumber,
    string? DefaultText,
    bool IsArchived,
    long Version);
