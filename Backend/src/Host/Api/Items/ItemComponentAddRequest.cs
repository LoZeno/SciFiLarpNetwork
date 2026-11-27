using System.ComponentModel.DataAnnotations;
using System.Text.Json.Serialization;

namespace SciFiLarpNetwork.Backend.Api.Items;

/// <param name="Version">The item version the change is based on; a stale version is rejected.</param>
[JsonUnmappedMemberHandling(JsonUnmappedMemberHandling.Disallow)]
public sealed record ItemComponentAddRequest(
    [Range(1, long.MaxValue)] long Version,
    [property: JsonRequired] Guid ItemId,
    [property: JsonRequired] int Quantity);
