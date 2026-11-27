using System.Text.Json.Serialization;

namespace SciFiLarpNetwork.Backend.Api.Items;

[JsonUnmappedMemberHandling(JsonUnmappedMemberHandling.Disallow)]
public sealed record ItemComponentRequest([property: JsonRequired] Guid ItemId, [property: JsonRequired] int Quantity);
