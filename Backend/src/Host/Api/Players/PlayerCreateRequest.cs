using System.ComponentModel.DataAnnotations;
using System.Text.Json.Serialization;
using SciFiLarpNetwork.Backend.Application.Players;
using SciFiLarpNetwork.Backend.Domain.Players;

namespace SciFiLarpNetwork.Backend.Api.Players;

[JsonUnmappedMemberHandling(JsonUnmappedMemberHandling.Disallow)]
public sealed record PlayerCreateRequest(
    [Required] string DisplayName,
    string? Email,
    string? Phone,
    string? Notes,
    PlayerType Type)
{
    public PlayerDefinition ToDefinition() => new(DisplayName, Email, Phone, Notes, Type);
}
