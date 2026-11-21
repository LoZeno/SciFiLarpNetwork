using System.Text.Json.Serialization;

namespace SciFiLarpNetwork.Backend.Api.LarpEvents;

[JsonUnmappedMemberHandling(JsonUnmappedMemberHandling.Disallow)]
public sealed record DayAddRequest([property: JsonRequired] DateOnly Date);
