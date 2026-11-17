using SciFiLarpNetwork.Backend.Domain.Players;

namespace SciFiLarpNetwork.Backend.Infrastructure.Players.PlayerDocuments;

public sealed record PlayerDocument(
    Guid Id,
    int Number,
    string DisplayName,
    string NormalizedDisplayName,
    string? Email,
    string? Phone,
    string? Notes,
    PlayerType Type,
    PlayerStatus Status,
    bool IsArchived)
{
    /// <summary>The player stream version, set by Marten for aggregate projection documents.</summary>
    public long Version { get; set; }
}
