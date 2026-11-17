using SciFiLarpNetwork.Backend.Domain.Players;

namespace SciFiLarpNetwork.Backend.Domain.Events.PlayerEvents;

public sealed record PlayerDetailsChanged(
    Guid PlayerId,
    string DisplayName,
    string NormalizedDisplayName,
    string? Email,
    string? Phone,
    string? Notes,
    PlayerType Type,
    DateTimeOffset OccurredAtUtc);
