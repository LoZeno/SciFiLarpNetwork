using System.Text.Json.Serialization;

namespace SciFiLarpNetwork.Management.Api.Siren;

public sealed record SirenActionField(
    [property: JsonPropertyName("name")] string Name,
    [property: JsonPropertyName("type")] string? Type,
    [property: JsonPropertyName("value")] string? Value,
    [property: JsonPropertyName("title")] string? Title);
