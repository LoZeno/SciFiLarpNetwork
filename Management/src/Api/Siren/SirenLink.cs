using System.Text.Json.Serialization;

namespace SciFiLarpNetwork.Management.Api.Siren;

public sealed record SirenLink(
    [property: JsonPropertyName("rel")] IReadOnlyList<string>? Rel,
    [property: JsonPropertyName("href")] string? Href,
    [property: JsonPropertyName("title")] string? Title);
