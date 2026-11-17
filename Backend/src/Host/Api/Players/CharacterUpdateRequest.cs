using System.ComponentModel.DataAnnotations;
using System.Text.Json.Serialization;
using SciFiLarpNetwork.Backend.Application.Players;
using SciFiLarpNetwork.Backend.Domain.Characters;

namespace SciFiLarpNetwork.Backend.Api.Players;

/// <param name="Version">The character version the edit is based on; a stale version is rejected.</param>
/// <param name="DeathDate">Required when <paramref name="Status"/> is Dead; ignored otherwise.</param>
/// <param name="StatValues">Values to set; the character's values for stats not listed are unchanged.</param>
[JsonUnmappedMemberHandling(JsonUnmappedMemberHandling.Disallow)]
public sealed record CharacterUpdateRequest(
    [Range(1, long.MaxValue)] long Version,
    [Required] string Name,
    string? Title,
    string? Species,
    string? Gender,
    string? Type,
    int ExperiencePoints,
    CharacterStatus Status,
    DateOnly? DeathDate,
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
            Status,
            DeathDate,
            NetworkId,
            (StatValues ?? []).Select(value => value.ToInput()).ToArray(),
            (NewStats ?? []).Select(stat => stat.ToNewStat()).ToArray());
}
