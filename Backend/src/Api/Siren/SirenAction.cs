using System.Text.Json.Serialization;

namespace SciFiLarpNetwork.Backend.Api.Siren;

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
