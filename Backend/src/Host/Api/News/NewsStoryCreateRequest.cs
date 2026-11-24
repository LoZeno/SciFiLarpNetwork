using System.Text.Json.Serialization;
using SciFiLarpNetwork.Backend.Application.News;

namespace SciFiLarpNetwork.Backend.Api.News;

/// <param name="StoryId">Chosen by the client once per story, so a repeated submission does not create it twice.</param>
/// <param name="StartsAt">An instant with its offset; stored in UTC.</param>
/// <param name="EndsAt">An instant with its offset; stored in UTC.</param>
[JsonUnmappedMemberHandling(JsonUnmappedMemberHandling.Disallow)]
public sealed record NewsStoryCreateRequest(
    [property: JsonRequired] Guid StoryId,
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
