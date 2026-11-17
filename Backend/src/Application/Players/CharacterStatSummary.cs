using SciFiLarpNetwork.Backend.Domain.Characters;

namespace SciFiLarpNetwork.Backend.Application.Players;

/// <param name="DefaultNumber">
/// For a Number stat, the value typed when the stat was defined; characters without their own value have it.
/// </param>
/// <param name="DefaultText">
/// For a Text stat, the value typed when the stat was defined; characters without their own value have it.
/// </param>
public sealed record CharacterStatSummary(
    Guid StatId,
    string Name,
    CharacterStatType Type,
    int? DefaultNumber,
    string? DefaultText,
    bool IsArchived,
    long Version);
