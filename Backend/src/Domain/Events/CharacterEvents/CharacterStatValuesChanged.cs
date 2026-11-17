using SciFiLarpNetwork.Backend.Domain.Characters;

namespace SciFiLarpNetwork.Backend.Domain.Events.CharacterEvents;

/// <summary>Sets the listed stat values; the character's values for other stats are unchanged.</summary>
public sealed record CharacterStatValuesChanged(
    Guid CharacterId,
    IReadOnlyList<CharacterStatValue> Values,
    DateTimeOffset OccurredAtUtc);
