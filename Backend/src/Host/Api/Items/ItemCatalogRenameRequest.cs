using System.ComponentModel.DataAnnotations;
using System.Text.Json.Serialization;

namespace SciFiLarpNetwork.Backend.Api.Items;

/// <summary>Renames an item type or a rarity.</summary>
/// <param name="Version">The version the rename is based on; a stale version is rejected.</param>
[JsonUnmappedMemberHandling(JsonUnmappedMemberHandling.Disallow)]
public sealed record ItemCatalogRenameRequest([Range(1, long.MaxValue)] long Version, [Required] string Name);
