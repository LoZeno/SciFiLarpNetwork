using System.Text.Json.Serialization;

namespace SciFiLarpNetwork.Backend.Api.CharacterCards;

/// <param name="CharacterIds">The characters to print, in print order; a repeated identifier prints another copy.</param>
[JsonUnmappedMemberHandling(JsonUnmappedMemberHandling.Disallow)]
public sealed record CharacterCardsRequest([property: JsonRequired] IReadOnlyList<Guid> CharacterIds);
