using System.Text.Json.Serialization;

namespace SciFiLarpNetwork.Backend.Api.LarpEvents;

/// <param name="DayId">Print the characters registered for this day; <see langword="null"/> prints the whole event.</param>
[JsonUnmappedMemberHandling(JsonUnmappedMemberHandling.Disallow)]
public sealed record LarpEventCardsRequest(Guid? DayId);
