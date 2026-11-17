using System.ComponentModel.DataAnnotations;
using System.Text.Json.Serialization;
using SciFiLarpNetwork.Backend.Application.Players;
using SciFiLarpNetwork.Backend.Domain.Characters;

namespace SciFiLarpNetwork.Backend.Api.Players;

/// <summary>A new character is always Active.</summary>
/// <param name="NetworkId">The optional non-default network.</param>
[JsonUnmappedMemberHandling(JsonUnmappedMemberHandling.Disallow)]
public sealed record CharacterCreateRequest(
    [Required] string Name,
    string? Title,
    string? Species,
    string? Gender,
    string? Type,
    int ExperiencePoints,
    Guid? NetworkId,
    IReadOnlyList<CharacterStatValueRequest>? StatValues,
    IReadOnlyList<NewCharacterStatRequest>? NewStats)
{
    public CharacterDefinition ToDefinition() =>
        new(
            Name,
            Title,
            Species,
            Gender,
            Type,
            ExperiencePoints,
            CharacterStatus.Active,
            DeathDate: null,
            NetworkId,
            (StatValues ?? []).Select(value => value.ToInput()).ToArray(),
            (NewStats ?? []).Select(stat => stat.ToNewStat()).ToArray());
}
