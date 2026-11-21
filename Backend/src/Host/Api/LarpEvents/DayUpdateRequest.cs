using System.Text.Json.Serialization;

namespace SciFiLarpNetwork.Backend.Api.LarpEvents;

/// <param name="Experience">The experience the day can give when the event closes; <see langword="null"/> for none.</param>
[JsonUnmappedMemberHandling(JsonUnmappedMemberHandling.Disallow)]
public sealed record DayUpdateRequest(
    [property: JsonRequired] Guid DayId,
    [property: JsonRequired] DateOnly Date,
    int? Experience);
