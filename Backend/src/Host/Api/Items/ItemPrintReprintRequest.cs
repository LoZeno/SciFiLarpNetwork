using System.Text.Json.Serialization;

namespace SciFiLarpNetwork.Backend.Api.Items;

/// <param name="PrintId">The earlier print whose copies, of every item it covered, are printed again.</param>
[JsonUnmappedMemberHandling(JsonUnmappedMemberHandling.Disallow)]
public sealed record ItemPrintReprintRequest([property: JsonRequired] Guid PrintId);
