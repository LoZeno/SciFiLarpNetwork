using System.Text.Json.Serialization;

namespace SciFiLarpNetwork.Backend.Api.Items;

/// <param name="PrintId">Chosen by the client once per print, so a repeated request makes no extra copies.</param>
/// <param name="ItemIds">The items to print, in print order.</param>
/// <param name="CopiesPerItem">How many copies of every item to make, from 1 to 50.</param>
[JsonUnmappedMemberHandling(JsonUnmappedMemberHandling.Disallow)]
public sealed record ItemPrintRequest(
    [property: JsonRequired] Guid PrintId,
    [property: JsonRequired] IReadOnlyList<Guid> ItemIds,
    [property: JsonRequired] int CopiesPerItem);
