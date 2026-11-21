using System.ComponentModel.DataAnnotations;
using System.Text.Json.Serialization;
using SciFiLarpNetwork.Backend.Application.LarpEvents;

namespace SciFiLarpNetwork.Backend.Api.LarpEvents;

/// <param name="Version">The event version the edit is based on; a stale version is rejected.</param>
/// <param name="BaseExperience">The experience every chosen participant can get when the event closes.</param>
[JsonUnmappedMemberHandling(JsonUnmappedMemberHandling.Disallow)]
public sealed record LarpEventUpdateRequest(
    [Range(1, long.MaxValue)] long Version,
    [Required] string Name,
    string? Description,
    int? BaseExperience)
{
    public LarpEventDefinition ToDefinition() => new(Name, Description, BaseExperience);
}
