using System.Text.Json.Serialization;

namespace SciFiLarpNetwork.Backend.Api.Siren;

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
