using SciFiLarpNetwork.Backend.Domain.Players;

namespace SciFiLarpNetwork.Backend.Application.Players;

public sealed record PlayerSummary(
    Guid PlayerId,
    int Number,
    string DisplayName,
    string? Email,
    string? Phone,
    string? Notes,
    PlayerType Type,
    PlayerStatus Status,
    bool IsArchived,
    long Version);
