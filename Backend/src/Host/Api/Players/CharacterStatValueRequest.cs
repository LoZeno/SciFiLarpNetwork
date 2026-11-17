using System.Text.Json.Serialization;
using SciFiLarpNetwork.Backend.Application.Players;

namespace SciFiLarpNetwork.Backend.Api.Players;

/// <summary>A value for an existing stat: <see cref="Number"/> for a Number stat, <see cref="Text"/> for a Text stat.</summary>
[JsonUnmappedMemberHandling(JsonUnmappedMemberHandling.Disallow)]
public sealed record CharacterStatValueRequest(Guid StatId, int? Number, string? Text)
{
    public CharacterStatInput ToInput() => new(StatId, Number, Text);
}
