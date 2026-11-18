using System.Text.Json.Serialization;
using SciFiLarpNetwork.Backend.Domain.CharacterSkills;

namespace SciFiLarpNetwork.Backend.Api.Players;

/// <param name="TalentId">The character's talent the skill is bought through.</param>
/// <param name="Count">How many times the skill is bought; above 1 only for multi-purchase skills.</param>
[JsonUnmappedMemberHandling(JsonUnmappedMemberHandling.Disallow)]
public sealed record SkillPurchaseRequest(
    [property: JsonRequired] Guid SkillId,
    [property: JsonRequired] Guid TalentId,
    [property: JsonRequired] int Count,
    string? Specifics)
{
    public SkillPurchase ToPurchase() => new(SkillId, TalentId, Count, Specifics);
}
