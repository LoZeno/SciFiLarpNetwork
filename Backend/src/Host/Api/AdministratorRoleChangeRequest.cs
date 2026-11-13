using System.ComponentModel.DataAnnotations;
using System.Text.Json.Serialization;

namespace SciFiLarpNetwork.Backend.Api;

[JsonUnmappedMemberHandling(JsonUnmappedMemberHandling.Disallow)]
public sealed record AdministratorRoleChangeRequest(
    [Required]
    [RegularExpression("^(reader|administrator)$")]
    string Role);
