using SciFiLarpNetwork.Backend.Domain.Characters;

namespace SciFiLarpNetwork.Backend.Domain.Events.CharacterStatEvents;

/// <summary>
/// A new stat, part of every character from now on. The value typed when it was defined is its default: every
/// character without its own value has <paramref name="DefaultNumber"/> (Number stats) or
/// <paramref name="DefaultText"/> (Text stats).
/// </summary>
public sealed record CharacterStatDefined(
    Guid StatId,
    string Name,
    string NormalizedName,
    CharacterStatType Type,
    int? DefaultNumber,
    string? DefaultText,
    DateTimeOffset OccurredAtUtc);
