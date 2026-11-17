using System.ComponentModel.DataAnnotations;
using System.Text.Json.Serialization;
using SciFiLarpNetwork.Backend.Application.Players;
using SciFiLarpNetwork.Backend.Domain.Characters;

namespace SciFiLarpNetwork.Backend.Api.Players;

/// <summary>A stat typed in the character dialog; a new name defines the stat for every character.</summary>
[JsonUnmappedMemberHandling(JsonUnmappedMemberHandling.Disallow)]
public sealed record NewCharacterStatRequest([Required] string Name, CharacterStatType Type, int? Number, string? Text)
{
    public NewCharacterStat ToNewStat() => new(Name, Type, Number, Text);
}
