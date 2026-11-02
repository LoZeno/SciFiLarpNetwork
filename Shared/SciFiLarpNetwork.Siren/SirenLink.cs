using System.Text.Json.Serialization;

namespace SciFiLarpNetwork.Common.Siren;

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
