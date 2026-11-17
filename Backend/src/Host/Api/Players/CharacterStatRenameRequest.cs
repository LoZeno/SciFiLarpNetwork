using System.ComponentModel.DataAnnotations;
using System.Text.Json.Serialization;

namespace SciFiLarpNetwork.Backend.Api.Players;

/// <param name="Version">The stat version the rename is based on; a stale version is rejected.</param>
[JsonUnmappedMemberHandling(JsonUnmappedMemberHandling.Disallow)]
public sealed record CharacterStatRenameRequest([Range(1, long.MaxValue)] long Version, [Required] string Name);
