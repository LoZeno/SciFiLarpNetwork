using System.ComponentModel.DataAnnotations;
using System.Text.Json.Serialization;
using SciFiLarpNetwork.Backend.Application.Networks;
using SciFiLarpNetwork.Backend.Domain.Networks;

namespace SciFiLarpNetwork.Backend.Api.Networks;

/// <param name="Version">The network version the edit is based on; a stale version is rejected.</param>
[JsonUnmappedMemberHandling(JsonUnmappedMemberHandling.Disallow)]
public sealed record NetworkUpdateRequest(
    [Range(1, long.MaxValue)] long Version,
    [Required] string Name,
    [StringLength(NetworkLimits.MaximumDescriptionLength)] string? Description,
    NetworkTheme Theme)
{
    public NetworkDefinition ToDefinition() => new(Name, Description, Theme);
}
