using System.ComponentModel.DataAnnotations;
using System.Text.Json.Serialization;
using SciFiLarpNetwork.Backend.Application.Players;
using SciFiLarpNetwork.Backend.Domain.Players;

namespace SciFiLarpNetwork.Backend.Api.Players;

/// <param name="Version">The player version the edit is based on; a stale version is rejected.</param>
[JsonUnmappedMemberHandling(JsonUnmappedMemberHandling.Disallow)]
public sealed record PlayerUpdateRequest(
    [Range(1, long.MaxValue)] long Version,
    [Required] string DisplayName,
    string? Email,
    string? Phone,
    string? Notes,
    PlayerType Type)
{
    public PlayerDefinition ToDefinition() => new(DisplayName, Email, Phone, Notes, Type);
}
