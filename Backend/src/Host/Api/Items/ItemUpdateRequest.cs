using System.ComponentModel.DataAnnotations;
using System.Text.Json.Serialization;
using SciFiLarpNetwork.Backend.Application.Items;

namespace SciFiLarpNetwork.Backend.Api.Items;

/// <param name="Version">The item version the edit is based on; a stale version is rejected.</param>
[JsonUnmappedMemberHandling(JsonUnmappedMemberHandling.Disallow)]
public sealed record ItemUpdateRequest(
    [Range(1, long.MaxValue)] long Version,
    [Required] string Name,
    [property: JsonRequired] Guid ItemTypeId,
    [property: JsonRequired] Guid RarityId,
    string? Description,
    string? Effect,
    [property: JsonRequired] int Cost,
    int? Charges,
    DateOnly? ExpiresOn)
{
    public ItemDefinition ToDefinition() => new(Name, ItemTypeId, RarityId, Description, Effect, Cost, Charges, ExpiresOn);
}
