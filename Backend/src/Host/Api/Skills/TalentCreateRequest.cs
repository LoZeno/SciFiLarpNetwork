using System.ComponentModel.DataAnnotations;
using System.Text.Json.Serialization;

namespace SciFiLarpNetwork.Backend.Api.Skills;

[JsonUnmappedMemberHandling(JsonUnmappedMemberHandling.Disallow)]
public sealed record TalentCreateRequest(
    [Required] string Name,
    [StringLength(SkillCatalogLimits.MaximumDescriptionLength)] string? Description);
