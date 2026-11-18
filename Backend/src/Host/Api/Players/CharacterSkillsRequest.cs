using System.ComponentModel.DataAnnotations;
using System.Text.Json.Serialization;
using SciFiLarpNetwork.Backend.Application.Players;

namespace SciFiLarpNetwork.Backend.Api.Players;

/// <param name="Version">The character version the edit is based on; a stale version is rejected.</param>
/// <param name="TalentIds">Every talent of the character, replacing the current ones.</param>
/// <param name="Skills">Every skill the character owns, replacing the current ones.</param>
[JsonUnmappedMemberHandling(JsonUnmappedMemberHandling.Disallow)]
public sealed record CharacterSkillsRequest(
    [Range(1, long.MaxValue)] long Version,
    [property: JsonRequired] IReadOnlyList<Guid> TalentIds,
    [property: JsonRequired] IReadOnlyList<SkillPurchaseRequest> Skills)
{
    public CharacterSkillsDefinition ToDefinition() =>
        new(TalentIds, Skills.Select(skill => skill.ToPurchase()).ToArray());
}
