using System.Text.Json;
using System.Text.Json.Serialization;

namespace SciFiLarpNetwork.Management.Api.Siren;

public sealed record SirenEntity(
    [property: JsonPropertyName("class")] IReadOnlyList<string>? Class,
    [property: JsonPropertyName("properties")] JsonElement? Properties,
    [property: JsonPropertyName("links")] IReadOnlyList<SirenLink>? Links,
    [property: JsonPropertyName("actions")] IReadOnlyList<SirenAction>? Actions);
