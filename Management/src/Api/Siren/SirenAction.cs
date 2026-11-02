using System.Text.Json.Serialization;

namespace SciFiLarpNetwork.Management.Api.Siren;

public sealed record SirenAction(
    [property: JsonPropertyName("name")] string? Name,
    [property: JsonPropertyName("href")] string? Href,
    [property: JsonPropertyName("method")] string? Method,
    [property: JsonPropertyName("type")] string? Type,
    [property: JsonPropertyName("title")] string? Title,
    [property: JsonPropertyName("fields")] IReadOnlyList<SirenActionField>? Fields);
