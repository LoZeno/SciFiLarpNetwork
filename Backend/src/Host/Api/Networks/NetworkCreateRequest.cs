using System.ComponentModel.DataAnnotations;
using System.Text.Json.Serialization;
using SciFiLarpNetwork.Backend.Application.Networks;
using SciFiLarpNetwork.Backend.Domain.Networks;

namespace SciFiLarpNetwork.Backend.Api.Networks;

/// <param name="Code">Fixed once the network exists: 1–32 lower-case letters, digits, and hyphens.</param>
[JsonUnmappedMemberHandling(JsonUnmappedMemberHandling.Disallow)]
public sealed record NetworkCreateRequest(
    [Required] string Code,
    [Required] string Name,
    string? Description,
    NetworkTheme Theme)
{
    public NetworkDefinition ToDefinition() => new(Name, Description, Theme);
}
