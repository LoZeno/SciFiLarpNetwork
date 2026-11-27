using System.Text.Json.Serialization;

namespace SciFiLarpNetwork.Backend.Api.Items;

/// <param name="CopyIds">The copies to print again, in print order.</param>
[JsonUnmappedMemberHandling(JsonUnmappedMemberHandling.Disallow)]
public sealed record ItemReprintRequest([property: JsonRequired] IReadOnlyList<Guid> CopyIds);
