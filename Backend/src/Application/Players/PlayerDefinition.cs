using SciFiLarpNetwork.Backend.Domain.Players;

namespace SciFiLarpNetwork.Backend.Application.Players;

/// <summary>The editable details of a player. Status and archiving have their own commands.</summary>
public sealed record PlayerDefinition(
    string DisplayName,
    string? Email,
    string? Phone,
    string? Notes,
    PlayerType Type);
