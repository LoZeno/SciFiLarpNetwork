using System.Text.Json;
using System.Text.Json.Serialization;

namespace SciFiLarpNetwork.Backend.Api;

public sealed record SirenEntity<TProperties>
{
    [JsonPropertyName("class")]
    public required IReadOnlyList<string> Class { get; init; }

    [JsonPropertyName("properties")]
    public TProperties? Properties { get; init; }

    [JsonPropertyName("entities")]
    public IReadOnlyList<SirenSubEntity>? Entities { get; init; }

    [JsonPropertyName("actions")]
    public IReadOnlyList<SirenAction>? Actions { get; init; }

    [JsonPropertyName("links")]
    public IReadOnlyList<SirenLink>? Links { get; init; }

    [JsonPropertyName("title")]
    public string? Title { get; init; }
}

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

public sealed record SirenLink
{
    [JsonPropertyName("rel")]
    public required IReadOnlyList<string> Rel { get; init; }

    [JsonPropertyName("href")]
    public required string Href { get; init; }

    [JsonPropertyName("class")]
    public IReadOnlyList<string>? Class { get; init; }

    [JsonPropertyName("type")]
    public string? Type { get; init; }

    [JsonPropertyName("title")]
    public string? Title { get; init; }
}

public sealed record SirenAction
{
    [JsonPropertyName("name")]
    public required string Name { get; init; }

    [JsonPropertyName("href")]
    public required string Href { get; init; }

    [JsonPropertyName("method")]
    public string Method { get; init; } = "GET";

    [JsonPropertyName("title")]
    public string? Title { get; init; }

    [JsonPropertyName("type")]
    public string? Type { get; init; }

    [JsonPropertyName("class")]
    public IReadOnlyList<string>? Class { get; init; }

    [JsonPropertyName("fields")]
    public IReadOnlyList<SirenField>? Fields { get; init; }
}

public sealed record SirenField
{
    [JsonPropertyName("name")]
    public required string Name { get; init; }

    [JsonPropertyName("type")]
    public string Type { get; init; } = "text";

    [JsonPropertyName("value")]
    public JsonElement? Value { get; init; }

    [JsonPropertyName("title")]
    public string? Title { get; init; }

    [JsonPropertyName("class")]
    public IReadOnlyList<string>? Class { get; init; }
}
