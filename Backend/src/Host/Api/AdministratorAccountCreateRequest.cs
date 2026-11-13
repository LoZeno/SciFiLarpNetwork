using System.ComponentModel.DataAnnotations;
using System.Text.Json.Serialization;

namespace SciFiLarpNetwork.Backend.Api;

[JsonUnmappedMemberHandling(JsonUnmappedMemberHandling.Disallow)]
public sealed record AdministratorAccountCreateRequest(
    [Required]
    [StringLength(64, MinimumLength = 3)]
    string UserName,
    [Required]
    [StringLength(128, MinimumLength = 12)]
    string Password,
    [Required]
    [RegularExpression("^(reader|administrator)$")]
    string Role);
