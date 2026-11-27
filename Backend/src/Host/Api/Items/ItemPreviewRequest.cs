using System.Text.Json.Serialization;

namespace SciFiLarpNetwork.Backend.Api.Items;

[JsonUnmappedMemberHandling(JsonUnmappedMemberHandling.Disallow)]
public sealed record ItemPreviewRequest(
    [property: JsonRequired] IReadOnlyList<Guid> ItemIds,
    [property: JsonRequired] int CopiesPerItem);
