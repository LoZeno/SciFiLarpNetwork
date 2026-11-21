using System.ComponentModel.DataAnnotations;
using System.Text.Json.Serialization;
using SciFiLarpNetwork.Backend.Application.LarpEvents;

namespace SciFiLarpNetwork.Backend.Api.LarpEvents;

[JsonUnmappedMemberHandling(JsonUnmappedMemberHandling.Disallow)]
public sealed record LarpEventCreateRequest([Required] string Name, string? Description)
{
    public LarpEventDefinition ToDefinition() => new(Name, Description);
}
