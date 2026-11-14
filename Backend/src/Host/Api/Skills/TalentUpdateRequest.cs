using System.ComponentModel.DataAnnotations;
using System.Text.Json.Serialization;

namespace SciFiLarpNetwork.Backend.Api.Skills;

/// <param name="Version">The talent version the edit is based on; a stale version is rejected.</param>
[JsonUnmappedMemberHandling(JsonUnmappedMemberHandling.Disallow)]
public sealed record TalentUpdateRequest(
    [Range(1, long.MaxValue)] long Version,
    [Required] string Name,
    [StringLength(SkillCatalogLimits.MaximumDescriptionLength)] string? Description);
