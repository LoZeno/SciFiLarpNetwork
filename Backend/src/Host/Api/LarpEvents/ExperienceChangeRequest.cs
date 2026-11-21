using System.Text.Json.Serialization;

namespace SciFiLarpNetwork.Backend.Api.LarpEvents;

/// <param name="Amount">The whole-number change, positive or negative, never zero.</param>
[JsonUnmappedMemberHandling(JsonUnmappedMemberHandling.Disallow)]
public sealed record ExperienceChangeRequest(
    [property: JsonRequired] Guid CharacterId,
    [property: JsonRequired] int Amount);
