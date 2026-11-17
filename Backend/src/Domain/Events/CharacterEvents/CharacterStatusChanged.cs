using SciFiLarpNetwork.Backend.Domain.Characters;

namespace SciFiLarpNetwork.Backend.Domain.Events.CharacterEvents;

/// <param name="DeathDate">Set only when <paramref name="Status"/> is <see cref="CharacterStatus.Dead"/>.</param>
public sealed record CharacterStatusChanged(
    Guid CharacterId,
    CharacterStatus Status,
    DateOnly? DeathDate,
    DateTimeOffset OccurredAtUtc);
