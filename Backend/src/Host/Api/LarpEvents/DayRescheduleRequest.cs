using System.Text.Json.Serialization;

namespace SciFiLarpNetwork.Backend.Api.LarpEvents;

[JsonUnmappedMemberHandling(JsonUnmappedMemberHandling.Disallow)]
public sealed record DayRescheduleRequest([property: JsonRequired] Guid DayId, [property: JsonRequired] DateOnly Date);
