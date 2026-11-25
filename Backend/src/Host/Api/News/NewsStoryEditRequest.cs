using System.ComponentModel.DataAnnotations;
using System.Text.Json.Serialization;
using SciFiLarpNetwork.Backend.Application.News;

namespace SciFiLarpNetwork.Backend.Api.News;

/// <param name="Version">The story version the edit is based on; a stale version is rejected.</param>
/// <param name="StartsAt">An instant with its offset; stored in UTC.</param>
/// <param name="EndsAt">An instant with its offset; stored in UTC.</param>
[JsonUnmappedMemberHandling(JsonUnmappedMemberHandling.Disallow)]
public sealed record NewsStoryEditRequest(
    [Range(1, long.MaxValue)] long Version,
    [property: JsonRequired] Guid NetworkId,
    [property: JsonRequired] Guid AuthorId,
    string? Title,
    string? Body,
    [property: JsonRequired] DateTimeOffset StartsAt,
    [property: JsonRequired] DateTimeOffset EndsAt,
    [property: JsonRequired] int HackingDifficulty)
{
    public NewsStoryContent ToContent() =>
        new(NetworkId, AuthorId, Title, Body, StartsAt, EndsAt, HackingDifficulty);
}
