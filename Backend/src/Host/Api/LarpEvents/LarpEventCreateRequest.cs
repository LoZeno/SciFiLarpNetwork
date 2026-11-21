using System.ComponentModel.DataAnnotations;
using System.Text.Json.Serialization;
using SciFiLarpNetwork.Backend.Application.LarpEvents;

namespace SciFiLarpNetwork.Backend.Api.LarpEvents;

/// <param name="BaseExperience">The experience every chosen participant can get when the event closes.</param>
[JsonUnmappedMemberHandling(JsonUnmappedMemberHandling.Disallow)]
public sealed record LarpEventCreateRequest([Required] string Name, string? Description, int? BaseExperience)
{
    public LarpEventDefinition ToDefinition() => new(Name, Description, BaseExperience);
}
