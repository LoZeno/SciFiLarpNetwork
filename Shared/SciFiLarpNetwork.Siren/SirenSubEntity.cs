using System.Text.Json;
using System.Text.Json.Serialization;

namespace SciFiLarpNetwork.Common.Siren;

public sealed record SirenSubEntity
{
    [JsonPropertyName("class")]
    public IReadOnlyList<string>? Class { get; init; }

    [JsonPropertyName("rel")]
    public required IReadOnlyList<string> Rel { get; init; }

    [JsonPropertyName("href")]
    public string? Href { get; init; }

    [JsonPropertyName("type")]
    public string? Type { get; init; }

    [JsonPropertyName("properties")]
    public JsonElement? Properties { get; init; }

    [JsonPropertyName("entities")]
    public IReadOnlyList<SirenSubEntity>? Entities { get; init; }

    [JsonPropertyName("actions")]
    public IReadOnlyList<SirenAction>? Actions { get; init; }

    [JsonPropertyName("links")]
    public IReadOnlyList<SirenLink>? Links { get; init; }

    [JsonPropertyName("title")]
    public string? Title { get; init; }
}
