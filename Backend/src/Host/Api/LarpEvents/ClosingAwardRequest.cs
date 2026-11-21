using System.Text.Json.Serialization;

namespace SciFiLarpNetwork.Backend.Api.LarpEvents;

/// <param name="BaseExperience">Whether the character gets the event's base experience.</param>
/// <param name="DayIds">The registered days whose experience the character gets.</param>
[JsonUnmappedMemberHandling(JsonUnmappedMemberHandling.Disallow)]
public sealed record ClosingAwardRequest(
    [property: JsonRequired] Guid CharacterId,
    bool BaseExperience,
    [property: JsonRequired] IReadOnlyList<Guid> DayIds);
