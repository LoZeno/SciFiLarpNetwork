using System.Text.Json;
using System.Text.Json.Serialization;

namespace SciFiLarpNetwork.Common.Siren;

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
