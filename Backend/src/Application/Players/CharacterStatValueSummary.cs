using SciFiLarpNetwork.Backend.Domain.Characters;

namespace SciFiLarpNetwork.Backend.Application.Players;

/// <summary>
/// A character's value for one stat. A character with no value of its own shows the stat's default value;
/// <see cref="Number"/> is set only for Number stats and <see cref="Text"/> only for Text stats.
/// </summary>
public sealed record CharacterStatValueSummary(
    Guid StatId,
    string Name,
    CharacterStatType Type,
    int? Number,
    string? Text,
    bool IsArchived);
