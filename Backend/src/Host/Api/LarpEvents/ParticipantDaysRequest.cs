using System.Text.Json.Serialization;

namespace SciFiLarpNetwork.Backend.Api.LarpEvents;

/// <param name="DayIds">The days to register the character for, or to unregister them from.</param>
[JsonUnmappedMemberHandling(JsonUnmappedMemberHandling.Disallow)]
public sealed record ParticipantDaysRequest(
    [property: JsonRequired] Guid CharacterId,
    [property: JsonRequired] IReadOnlyList<Guid> DayIds);
