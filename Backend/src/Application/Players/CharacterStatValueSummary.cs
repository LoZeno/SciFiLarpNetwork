using SciFiLarpNetwork.Backend.Domain.Characters;

namespace SciFiLarpNetwork.Backend.Application.Players;

/// <summary>
/// A character's value for one stat. A character with no value shows 0 for a Number stat and an empty text for a
/// Text stat; <see cref="Number"/> is set only for Number stats and <see cref="Text"/> only for Text stats.
/// </summary>
public sealed record CharacterStatValueSummary(
    Guid StatId,
    string Name,
    CharacterStatType Type,
    int? Number,
    string? Text,
    bool IsArchived);
